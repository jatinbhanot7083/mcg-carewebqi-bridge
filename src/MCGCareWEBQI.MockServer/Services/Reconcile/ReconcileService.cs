namespace MCGCareWEBQI.MockServer.Services.Reconcile;

/// Stand-in for real MCG's Reconcile.asmx. Always returns a success ACK string —
/// the bridge logs it but generally does not need its contents.
public sealed class ReconcileService(ILogger<ReconcileService> log) : IReconcileService
{
    public string AcknowledgeMessageByEpisode(string EpisodeID)
    {
        log.LogInformation("Reconcile.AcknowledgeMessageByEpisode received EpisodeID={EpisodeID}", EpisodeID);
        return $"ACK:Episode:{EpisodeID}:{DateTime.UtcNow:O}";
    }

    public string AcknowledgeMessageByTransaction(int TransactionID)
    {
        log.LogInformation("Reconcile.AcknowledgeMessageByTransaction received TransactionID={TransactionID}", TransactionID);
        return $"ACK:Transaction:{TransactionID}:{DateTime.UtcNow:O}";
    }
}
