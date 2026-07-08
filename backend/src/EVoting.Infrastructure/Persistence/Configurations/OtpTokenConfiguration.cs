using EVoting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVoting.Infrastructure.Persistence.Configurations;

public class OtpTokenConfiguration : IEntityTypeConfiguration<OtpToken>
{
    public void Configure(EntityTypeBuilder<OtpToken> builder)
    {
        builder.ToTable("OtpTokens");

        builder.HasKey(o => o.OtpTokenId);

        builder.Property(o => o.OtpHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(o => o.ExpiresAt)
            .IsRequired();

        builder.Property(o => o.IsUsed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(o => o.CreatedAt)
            .IsRequired();

        builder.HasOne(o => o.User)
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
