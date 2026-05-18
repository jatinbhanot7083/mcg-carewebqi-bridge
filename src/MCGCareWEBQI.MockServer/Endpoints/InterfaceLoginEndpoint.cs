using MCGCareWEBQI.MockServer.Services;
using MCGCareWEBQI.Shared.Configuration;
using MCGCareWEBQI.Shared.Hashing;
using Microsoft.Extensions.Options;

namespace MCGCareWEBQI.MockServer.Endpoints;

/// Hosts the path the bridge POSTs to: /interface/interfacelogin.aspx
/// Matches the same path as real MCG so swapping is config-only.
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

            var session = store.Create(fields);
            log.LogInformation("Mock MCG session {Sid} created for episode {Ep} (requestType={Rt})",
                session.SessionId, session.EpisodeId, session.RequestType);

            // Land on the MCG "Enter Documentation" page (matches real CareWebQI flow).
            return Results.Redirect($"/cwqi/login/{session.SessionId}");
        });
    }
}
