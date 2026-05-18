using System.ComponentModel.DataAnnotations;

namespace MCGCareWEBQI.Data.Entities;

/// One row per state change or notable event. Independent of IntegrationTransaction
/// so we can write audit entries even when the parent row is being created.
public sealed class IntegrationAudit
{
    [Key]
    public long AuditId { get; set; }

    public Guid TransactionId { get; set; }

    [MaxLength(64)]
    public string EventType { get; set; } = "";

    public string? PayloadJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public IntegrationTransaction? Transaction { get; set; }
}
