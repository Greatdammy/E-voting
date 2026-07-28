using EVoting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVoting.Infrastructure.Persistence.Configurations;

public class IntegrityAlertConfiguration : IEntityTypeConfiguration<IntegrityAlert>
{
    public void Configure(EntityTypeBuilder<IntegrityAlert> builder)
    {
        builder.ToTable("IntegrityAlerts");

        builder.HasKey(a => a.AlertId);

        builder.Property(a => a.ReviewNote)
            .HasMaxLength(1000);

        builder.HasIndex(a => new { a.ElectionId, a.Status });

        builder.HasOne(a => a.Election)
            .WithMany()
            .HasForeignKey(a => a.ElectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.ReviewedByUser)
            .WithMany()
            .HasForeignKey(a => a.ReviewedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
