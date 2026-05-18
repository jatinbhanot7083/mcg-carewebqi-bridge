using System.Text.Json;
using MCGCareWEBQI.Data;
using MCGCareWEBQI.Shared.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MCGCareWEBQI.Bridge.Services;

/// Fires server-to-server callback POST when the caller supplied a callbackUrl.
/// Fire-and-forget with bounded retries. Failures are logged + audited but do not block the user-facing flow.
public sealed class CallbackService(
    IServiceScopeFactory scopeFactory,
    HttpClient http,
    IOptions<BridgeOptions> bridgeOptions,
    ILogger<CallbackService> log)
{
    public async Task FireAsync(Guid txnId)
    {
        var bridge = bridgeOptions.Value;
        if (!bridge.EnableServerCallback) return;

        using var scope = scopeFactory.CreateScope();
        var db          = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
        var integration = scope.ServiceProvider.GetRequiredService<IntegrationService>();

        var txn = await db.Transactions.FirstOrDefaultAsync(t => t.TransactionId == txnId);
        if (txn is null || string.IsNullOrEmpty(txn.CallbackUrl)) return;

        var result = await integration.GetResultAsync(txnId);
        if (result is null) return;
        var json = JsonSerializer.Serialize(result);

        for (var attempt = 1; attempt <= bridge.CallbackRetryCount; attempt++)
        {
            txn.CallbackAttempts = attempt;
            try
            {
                using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                using var resp    = await http.PostAsync(txn.CallbackUrl, content);
                if (resp.IsSuccessStatusCode)
                {
                    txn.CallbackDeliveredAt = DateTime.UtcNow;
                    txn.UpdatedAt           = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                    log.LogInformation("Callback delivered for {Txn} on attempt {Attempt}", txnId, attempt);
                    return;
                }
                log.LogWarning("Callback returned {Status} for {Txn} attempt {Attempt}", (int)resp.StatusCode, txnId, attempt);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Callback POST failed for {Txn} attempt {Attempt}", txnId, attempt);
            }
            if (attempt < bridge.CallbackRetryCount) await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
        }

        txn.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }
}
