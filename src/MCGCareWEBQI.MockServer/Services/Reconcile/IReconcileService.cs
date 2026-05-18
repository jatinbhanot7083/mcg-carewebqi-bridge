using CoreWCF;

namespace MCGCareWEBQI.MockServer.Services.Reconcile;

/// Mirrors MCG's Reconcile.asmx contract (namespace http://www.carewebqi.com/WS/Reconcile).
/// Bridge calls this after receiving a CwqiMessage to acknowledge the episode.
[ServiceContract(Namespace = "http://www.carewebqi.com/WS/Reconcile")]
public interface IReconcileService
{
    [OperationContract(Action = "http://www.carewebqi.com/WS/Reconcile/AcknowledgeMessageByEpisode")]
    string AcknowledgeMessageByEpisode(string EpisodeID);

    [OperationContract(Action = "http://www.carewebqi.com/WS/Reconcile/AcknowledgeMessageByTransaction")]
    string AcknowledgeMessageByTransaction(int TransactionID);
}
