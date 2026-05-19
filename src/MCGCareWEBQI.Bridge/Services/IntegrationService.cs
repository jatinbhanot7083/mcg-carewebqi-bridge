using System.Text.Json;
using MCGCareWEBQI.Data;
using MCGCareWEBQI.Data.Entities;
using MCGCareWEBQI.Shared.Configuration;
using MCGCareWEBQI.Shared.Models.Launch;
using MCGCareWEBQI.Shared.Models.Mcg;
using MCGCareWEBQI.Shared.Models.Result;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MCGCareWEBQI.Bridge.Services;

/// Orchestrates the lifecycle of one MCG documentation transaction:
///   InitiateAsync  → create row, build signed POST, return field list
///   IngestAsync    → parse CwqiMessage, persist, trigger ACK + callback
///   GetAsync       → poll/lookup
public sealed class IntegrationService(
    IntegrationDbContext db,
    IOptions<McgOptions> mcgOptions,
    IOptions<BridgeOptions> bridgeOptions,
    ReconcileSoapClient reconcile,
    CallbackService callback,
    ILogger<IntegrationService> log)
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    public async Task<(Guid txnId, IReadOnlyList<KeyValuePair<string, string>> fields, string interfaceLoginUrl)>
        InitiateAsync(LaunchRequest launch, CancellationToken ct = default)
    {
        var mcg    = mcgOptions.Value;
        var bridge = bridgeOptions.Value;

        // Cert-required audit (CareWebQI Cert v10.0 — episode re-launch + patient merge).
        // When a caller re-launches with the same episodeId, we record the fact so the cert
        // reviewer can see the bridge correctly preserved the episode ID across calls.
        // When the patient ID differs from a prior launch for the same episodeId, we record
        // that too — MCG will show its merge prompt; the bridge just needs allowPatientMerge=True.
        string? relaunchOf = null;
        string? mergeFromPatient = null;
        if (!string.IsNullOrEmpty(launch.EpisodeId))
        {
            var prior = await db.Transactions
                .Where(t => t.CallerId == launch.CallerId
                         && (t.McgResponseJson != null || t.OutboundFieldsJson != null)
                         && t.OutboundFieldsJson != null
                         && t.OutboundFieldsJson.Contains("\"episodeID\":\"" + launch.EpisodeId + "\""))
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync(ct);
            if (prior is not null)
            {
                relaunchOf = prior.TransactionId.ToString();
                // Detect patient ID change for the merge cert scenario.
                if (!string.IsNullOrEmpty(launch.PatientId) && prior.OutboundFieldsJson is not null
                    && !prior.OutboundFieldsJson.Contains("\"patientID\":\"" + launch.PatientId + "\""))
                {
                    mergeFromPatient = "prior";  // exact prior value extracted in audit payload below
                }
            }
        }

        var txn = new IntegrationTransaction
        {
            CallerId          = launch.CallerId,
            CallerTxnId       = launch.CallerTxnId,
            Status            = TransactionStatus.Initiated,
            RequestParamsJson = JsonSerializer.Serialize(launch, JsonOpts),
            CallbackUrl       = launch.CallbackUrl,
            ReturnContext     = launch.ReturnContext,
        };
        db.Transactions.Add(txn);
        db.Audits.Add(new IntegrationAudit { TransactionId = txn.TransactionId, EventType = "LaunchReceived" });
        if (relaunchOf is not null)
        {
            db.Audits.Add(new IntegrationAudit
            {
                TransactionId = txn.TransactionId,
                EventType     = mergeFromPatient is null ? "RelaunchEpisode" : "RelaunchEpisodeWithPatientMerge",
                PayloadJson   = $"{{\"priorTxn\":\"{relaunchOf}\",\"episodeId\":\"{launch.EpisodeId}\",\"newPatientId\":\"{launch.PatientId}\"}}"
            });
        }
        await db.SaveChangesAsync(ct);

        var returnUrl = $"{bridge.PublicBaseUrl.TrimEnd('/')}{bridge.ReceiverPath}";
        var fields    = McgRequestBuilder.Build(launch, mcg, returnUrl, cwqiTransactionId: txn.TransactionId.ToString());

        txn.OutboundFieldsJson = JsonSerializer.Serialize(
            fields.ToDictionary(kv => kv.Key, kv => kv.Value), JsonOpts);
        txn.Status    = TransactionStatus.Sent;
        txn.UpdatedAt = DateTime.UtcNow;
        db.Audits.Add(new IntegrationAudit
        {
            TransactionId = txn.TransactionId,
            EventType     = "OutboundFieldsBuilt",
            PayloadJson   = $"{{\"fieldCount\":{fields.Count}}}"
        });
        await db.SaveChangesAsync(ct);

        log.LogInformation("Initiated MCG launch txn {Txn} caller={Caller} callerTxnId={CallerTxn}",
            txn.TransactionId, launch.CallerId, launch.CallerTxnId);
        return (txn.TransactionId, fields, mcg.InterfaceLoginUrl);
    }

    public async Task<Guid?> IngestAsync(string responseXml, CancellationToken ct = default)
    {
        // CwqiMessage carries requestID matching the txn id we minted; cwqierror uses the requestid attribute.
        Guid? txnId = TryExtractRequestId(responseXml);

        var txn = txnId.HasValue
            ? await db.Transactions.FirstOrDefaultAsync(t => t.TransactionId == txnId.Value, ct)
            : await db.Transactions
                .Where(t => t.Status == TransactionStatus.Sent)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync(ct);

        if (txn is null)
        {
            log.LogWarning("Received CwqiMessage but no matching transaction. RequestId={Rid}", txnId);
            return null;
        }

        if (CwqiError.TryParse(responseXml, out var err) && err is not null)
        {
            txn.McgErrorXml   = responseXml;
            txn.Status        = TransactionStatus.Failed;
            txn.FailureReason = string.Join("; ", err.Messages.Select(m => $"{m.Code}: {m.Text}"));
            db.Audits.Add(new IntegrationAudit
            {
                TransactionId = txn.TransactionId,
                EventType     = "McgErrorReceived",
                PayloadJson   = JsonSerializer.Serialize(err.Messages, JsonOpts)
            });
        }
        else
        {
            var msg = CwqiMessage.Parse(responseXml);
            txn.McgResponseXml  = responseXml;
            txn.McgResponseJson = JsonSerializer.Serialize(msg, JsonOpts);
            txn.Status          = TransactionStatus.Returned;
            db.Audits.Add(new IntegrationAudit
            {
                TransactionId = txn.TransactionId,
                EventType     = "McgResponseReceived",
                PayloadJson   = $"{{\"episodeId\":\"{msg.EpisodeId}\"}}"
            });

            try
            {
                await reconcile.AcknowledgeEpisodeAsync(msg.EpisodeId ?? "", ct);
                txn.Status = TransactionStatus.Acknowledged;
                db.Audits.Add(new IntegrationAudit { TransactionId = txn.TransactionId, EventType = "ReconcileAcknowledged" });
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Reconcile ACK failed for txn {Txn}; continuing", txn.TransactionId);
                db.Audits.Add(new IntegrationAudit
                {
                    TransactionId = txn.TransactionId,
                    EventType     = "ReconcileAckFailed",
                    PayloadJson   = $"{{\"error\":\"{ex.Message.Replace("\"", "\\\"")}\"}}"
                });
            }
        }

        txn.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        if (!string.IsNullOrEmpty(txn.CallbackUrl))
        {
            _ = callback.FireAsync(txn.TransactionId);
        }

        return txn.TransactionId;
    }

    public async Task<IntegrationResult?> GetResultAsync(Guid txnId, CancellationToken ct = default)
    {
        var txn = await db.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.TransactionId == txnId, ct);
        if (txn is null) return null;

        var result = new IntegrationResult
        {
            TransactionId = txn.TransactionId,
            CallerId      = txn.CallerId,
            CallerTxnId   = txn.CallerTxnId,
            Status        = txn.Status,
            CreatedAt     = txn.CreatedAt,
            UpdatedAt     = txn.UpdatedAt,
            ReturnContext = txn.ReturnContext,
            FailureReason = txn.FailureReason,
        };
        if (!string.IsNullOrEmpty(txn.McgResponseXml)) result.McgResponse = CwqiMessage.Parse(txn.McgResponseXml);
        if (!string.IsNullOrEmpty(txn.McgErrorXml) && CwqiError.TryParse(txn.McgErrorXml, out var err)) result.McgError = err;

        // Integration-trace visibility (developer USP).
        result.OutboundFieldsJson = txn.OutboundFieldsJson;
        result.McgResponseXml     = txn.McgResponseXml;
        result.LaunchUrl          = BuildLaunchUrlFromParams(txn.RequestParamsJson);
        return result;
    }

    private static string? BuildLaunchUrlFromParams(string? requestParamsJson)
    {
        if (string.IsNullOrEmpty(requestParamsJson)) return null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(requestParamsJson);
            var qs = new List<string>();
            foreach (var p in doc.RootElement.EnumerateObject())
            {
                if (p.Value.ValueKind != System.Text.Json.JsonValueKind.String) continue;
                var v = p.Value.GetString();
                if (string.IsNullOrEmpty(v)) continue;
                qs.Add($"{char.ToLowerInvariant(p.Name[0])}{p.Name.Substring(1)}={Uri.EscapeDataString(v)}");
            }
            return qs.Count == 0 ? null : "/launch?" + string.Join("&", qs);
        }
        catch { return null; }
    }

    private static Guid? TryExtractRequestId(string xml)
    {
        try
        {
            var doc = System.Xml.Linq.XDocument.Parse(xml);
            var raw = doc.Root?.Attribute("requestID")?.Value
                   ?? doc.Root?.Attribute("requestid")?.Value;
            return Guid.TryParse(raw, out var g) ? g : null;
        }
        catch { return null; }
    }
}
