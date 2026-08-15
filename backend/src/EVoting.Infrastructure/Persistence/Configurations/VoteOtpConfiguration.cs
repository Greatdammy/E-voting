using EVoting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVoting.Infrastructure.Persistence.Configurations;

public class VoteOtpConfiguration : IEntityTypeConfiguration<VoteOtp>
{
    public void Configure(EntityTypeBuilder<VoteOtp> builder)
    {
        builder.ToTable("VoteOtps");

        builder.HasKey(o => o.VoteOtpId);

        builder.Property(o => o.CodeHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(o => o.AttemptCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(o => o.IsUsed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(o => new { o.UserId, o.ElectionId, o.CreatedAt });

        builder.HasOne(o => o.User)
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.Election)
            .WithMany()
            .HasForeignKey(o => o.ElectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}