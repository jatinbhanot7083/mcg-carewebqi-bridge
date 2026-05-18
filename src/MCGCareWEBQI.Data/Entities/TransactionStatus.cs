namespace MCGCareWEBQI.Data.Entities;

public static class TransactionStatus
{
    public const string Initiated    = "Initiated";    // Launch URL received, params validated
    public const string Sent         = "Sent";         // Form posted to MCG, awaiting return
    public const string Returned     = "Returned";     // MCG posted back CwqiMessage
    public const string Acknowledged = "Acknowledged"; // Reconcile.asmx ACK succeeded
    public const string Failed       = "Failed";       // MCG returned error, or any pipeline failure
}
