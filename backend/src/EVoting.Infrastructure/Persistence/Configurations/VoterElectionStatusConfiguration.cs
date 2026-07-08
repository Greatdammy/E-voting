using EVoting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVoting.Infrastructure.Persistence.Configurations;

public class VoterElectionStatusConfiguration : IEntityTypeConfiguration<VoterElectionStatus>
{
    public void Configure(EntityTypeBuilder<VoterElectionStatus> builder)
    {
        builder.ToTable("VoterElectionStatuses");

        builder.HasKey(v => new { v.UserId, v.ElectionId });

        builder.Property(v => v.HasVoted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasOne(v => v.User)
            .WithMany()
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.Election)
            .WithMany()
            .HasForeignKey(v => v.ElectionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
