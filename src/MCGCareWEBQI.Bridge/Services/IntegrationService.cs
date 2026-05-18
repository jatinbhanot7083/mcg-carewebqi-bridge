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
        return result;
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
