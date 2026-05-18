using System.ComponentModel.DataAnnotations;

namespace MCGCareWEBQI.Data.Entities;

/// One row per MCG launch. Holds everything the bridge knows about a transaction
/// from the moment a caller hits /launch through to the final MCG acknowledgement.
public sealed class IntegrationTransaction
{
    [Key]
    public Guid TransactionId { get; set; } = Guid.NewGuid();

    [MaxLength(64)]
    public string CallerId { get; set; } = "";

    [MaxLength(128)]
    public string CallerTxnId { get; set; } = "";

    [MaxLength(32)]
    public string Status { get; set; } = TransactionStatus.Initiated;

    /// All launch parameters from the caller, serialized as JSON. Audit trail.
    public string? RequestParamsJson { get; set; }

    /// Form fields posted to MCG (with messageHash). Audit trail.
    public string? OutboundFieldsJson { get; set; }

    /// Raw CwqiMessage XML returned by MCG.
    public string? McgResponseXml { get; set; }

    /// Structured CwqiMessage serialized as JSON for easy querying.
    public string? McgResponseJson { get; set; }

    /// Raw cwqierror XML if MCG rejected the request.
    public string? McgErrorXml { get; set; }

    [MaxLength(512)]
    public string? CallbackUrl { get; set; }

    public DateTime? CallbackDeliveredAt { get; set; }

    public int CallbackAttempts { get; set; }

    public string? ReturnContext { get; set; }

    public string? FailureReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<IntegrationAudit> Audits { get; set; } = [];
}
