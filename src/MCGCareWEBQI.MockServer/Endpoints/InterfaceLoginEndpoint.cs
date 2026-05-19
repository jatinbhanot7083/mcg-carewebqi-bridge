using MCGCareWEBQI.MockServer.Services;
using MCGCareWEBQI.Shared.Configuration;
using MCGCareWEBQI.Shared.Hashing;
using Microsoft.Extensions.Options;

namespace MCGCareWEBQI.MockServer.Endpoints;

/// Hosts the path the bridge POSTs to: /interface/interfacelogin.aspx
/// Matches the same path as real MCG so swapping is config-only.
///
/// Also implements the certification "patient merge" scenario (CWQI Cert Checklist v10.0, p82):
/// when a re-launch sends the SAME episodeID with a DIFFERENT patientID, we route to the
/// merge prompt page instead of the search/episode page.
public static class InterfaceLoginEndpoint
{
    public static void MapInterfaceLogin(this IEndpointRouteBuilder app)
    {
        app.MapPost("/interface/interfacelogin.aspx", async (
            HttpContext ctx,
            McgSessionStore store,
            IOptions<McgOptions> mcgOptions,
            ILogger<McgSessionStore> log) =>
        {
            if (!ctx.Request.HasFormContentType)
                return Results.BadRequest("Expected application/x-www-form-urlencoded form post.");

            var form = await ctx.Request.ReadFormAsync();
            var fields = form.ToDictionary(kv => kv.Key, kv => kv.Value.ToString(), StringComparer.Ordinal);

            if (!fields.TryGetValue("messageHash", out var suppliedHash) || string.IsNullOrEmpty(suppliedHash))
                return Results.BadRequest("Missing messageHash. Dev Guide §2.2 requires a signed request.");

            var mcg = mcgOptions.Value;
            var algo = CwqiHash.Parse(mcg.HashAlgorithm);
            var verifyFields = fields
                .Where(kv => !string.Equals(kv.Key, "messageHash", StringComparison.OrdinalIgnoreCase))
                .Select(kv => new KeyValuePair<string, string?>(kv.Key, kv.Value));

            if (!CwqiHash.Verify(verifyFields, mcg.LoginKey, suppliedHash, algo))
            {
                log.LogWarning("Rejected interface login: messageHash mismatch from {Ip}", ctx.Connection.RemoteIpAddress);
                return Results.Unauthorized();
            }

            var prefix = ctx.Request.PathBase.Value ?? "";

            // ---- Re-launch + patient merge detection (cert scenario) ----
            var incomingEpisode = fields.GetValueOrDefault("episodeID");
            var incomingPatient = fields.GetValueOrDefault("patientID");
            var allowMerge = string.Equals(fields.GetValueOrDefault("allowPatientMerge"), "True",
                                           StringComparison.OrdinalIgnoreCase);

            if (!string.IsNullOrEmpty(incomingEpisode))
            {
                var prior = store.FindByEpisode(incomingEpisode);
                if (prior is not null && !string.IsNullOrEmpty(incomingPatient)
                    && !string.Equals(prior.PatientId, incomingPatient, StringComparison.Ordinal))
                {
                    // Re-launch with same episode but different patient → merge required.
                    // Stash the proposed new patient on the existing session and route to the merge UI.
                    prior.PendingMergePatientId        = incomingPatient;
                    prior.PendingMergePatientFirstName = fields.GetValueOrDefault("patientFirstName");
                    prior.PendingMergePatientLastName  = fields.GetValueOrDefault("patientLastName");
                    prior.PendingMergePatientDob       = fields.GetValueOrDefault("patientDateOfBirth");
                    prior.MergeAllowed                 = allowMerge;
                    store.Save(prior);
                    log.LogInformation("Merge prompt triggered for episode {Ep} prior={Sid} newPatient={Pid}",
                        incomingEpisode, prior.SessionId, incomingPatient);
                    return Results.Redirect($"{prefix}/cwqi/merge/{prior.SessionId}");
                }
                if (prior is not null)
                {
                    // Same patient — re-open the existing session (cert: re-launch existing episode).
                    log.LogInformation("Re-launching existing episode {Ep} session {Sid}", incomingEpisode, prior.SessionId);
                    return Results.Redirect($"{prefix}/cwqi/episode/{prior.SessionId}");
                }
            }

            var session = store.Create(fields);
            log.LogInformation("Mock MCG session {Sid} created for episode {Ep} (requestType={Rt})",
                session.SessionId, session.EpisodeId, session.RequestType);

            // Real CareWebQI shows an "Enter Documentation" landing page first; we skip it and
            // drop the clinician straight into the guideline search (EvokeConnect's UX upgrade).
            // Include PathBase so dock-mode (proxied through bridge /__mcg) stays in the iframe.
            return Results.Redirect($"{prefix}/cwqi/search/{session.SessionId}");
        });
    }
}
