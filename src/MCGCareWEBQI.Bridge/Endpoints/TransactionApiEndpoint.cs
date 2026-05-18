using MCGCareWEBQI.Bridge.Services;

namespace MCGCareWEBQI.Bridge.Endpoints;

/// Polling endpoint. Callers that can't or don't want to use postMessage / webhook
/// can poll here for the final result keyed by the bridge-assigned transaction id.
public static class TransactionApiEndpoint
{
    public static void MapTransactionApi(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/transactions/{id:guid}", async (Guid id, IntegrationService svc) =>
        {
            var result = await svc.GetResultAsync(id);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });
    }
}
