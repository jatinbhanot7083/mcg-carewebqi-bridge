using MCGCareWEBQI.Bridge.Services;

namespace MCGCareWEBQI.Bridge.Endpoints;

/// Receives MCG's post-back. MCG (or the stub) posts a form with the cwqiresponse
/// field containing either a <CwqiMessage> or a <cwqierror> XML document.
public static class ReceiverEndpoint
{
    public static void MapReceiver(this IEndpointRouteBuilder app)
    {
        app.MapPost("/receive", async (HttpContext ctx, IntegrationService svc, ILogger<IntegrationService> log) =>
        {
            string xml;
            if (ctx.Request.HasFormContentType)
            {
                var form = await ctx.Request.ReadFormAsync();
                xml = form["cwqiresponse"].ToString();
                if (string.IsNullOrEmpty(xml))
                    xml = form["cwqierror"].ToString();
            }
            else
            {
                using var reader = new StreamReader(ctx.Request.Body);
                xml = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrWhiteSpace(xml))
            {
                log.LogWarning("/receive got empty body");
                return Results.BadRequest("Empty response body.");
            }

            var txnId = await svc.IngestAsync(xml);
            if (txnId is null) return Results.BadRequest("Could not associate response with a transaction.");

            return Results.Redirect($"/complete/{txnId}");
        }).DisableAntiforgery();
    }
}
