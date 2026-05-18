using MCGCareWEBQI.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MCGCareWEBQI.Data;

public sealed class IntegrationDbContext(DbContextOptions<IntegrationDbContext> options) : DbContext(options)
{
    public DbSet<IntegrationTransaction> Transactions => Set<IntegrationTransaction>();
    public DbSet<IntegrationAudit>       Audits       => Set<IntegrationAudit>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        var txn = mb.Entity<IntegrationTransaction>();
        txn.ToTable("IntegrationTransaction");
        txn.Property(x => x.CreatedAt).HasColumnType("datetime2");
        txn.Property(x => x.UpdatedAt).HasColumnType("datetime2");
        txn.Property(x => x.CallbackDeliveredAt).HasColumnType("datetime2");
        txn.HasIndex(x => new { x.CallerId, x.CallerTxnId }).IsUnique(false);
        txn.HasIndex(x => x.Status);
        txn.HasIndex(x => x.CreatedAt);

        var aud = mb.Entity<IntegrationAudit>();
        aud.ToTable("IntegrationAudit");
        aud.Property(x => x.CreatedAt).HasColumnType("datetime2");
        aud.HasOne(x => x.Transaction)
           .WithMany(x => x.Audits)
           .HasForeignKey(x => x.TransactionId)
           .OnDelete(DeleteBehavior.Cascade);
        aud.HasIndex(x => x.TransactionId);
        aud.HasIndex(x => x.CreatedAt);
    }
}
