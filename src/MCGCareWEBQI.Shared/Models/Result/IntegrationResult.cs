using MCGCareWEBQI.Shared.Models.Mcg;

namespace MCGCareWEBQI.Shared.Models.Result;

/// Bridge → Caller payload. Sent via:
///   - window.postMessage to opener (modern SPA flow)
///   - HTTP POST to CallbackUrl (server callback flow)
///   - JSON response to GET /api/transactions/{id} (polling flow)
public sealed class IntegrationResult
{
    public Guid TransactionId { get; set; }
    public string CallerId { get; set; } = "";
    public string CallerTxnId { get; set; } = "";
    public string Status { get; set; } = "";    // Initiated, Sent, Returned, Acknowledged, Failed
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// Echoed back unchanged if the caller passed returnContext on the launch URL.
    public string? ReturnContext { get; set; }

    /// Populated once MCG returns. Null while status is Initiated or Sent.
    public CwqiMessage? McgResponse { get; set; }

    /// Populated only on Failed status.
    public CwqiError? McgError { get; set; }
    public string? FailureReason { get; set; }

    // ---- Integration trace (developer visibility / demo USP) ----

    /// The full URL that opened the popup / iframe (with all query params except hash).
    public string? LaunchUrl { get; set; }

    /// The form-fields dictionary the bridge actually POSTed to MCG, JSON-serialized.
    /// Includes the messageHash and every Dev Guide §4 field that was sent.
    public string? OutboundFieldsJson { get; set; }

    /// The raw <CwqiMessage> XML the bridge received from MCG (or stub) on the post-back.
    public string? McgResponseXml { get; set; }
}
