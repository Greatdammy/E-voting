using EVoting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVoting.Infrastructure.Persistence.Configurations;

public class VoteConfiguration : IEntityTypeConfiguration<Vote>
{
    public void Configure(EntityTypeBuilder<Vote> builder)
    {
        builder.ToTable("Votes");

        builder.HasKey(v => v.VoteId);

        builder.Property(v => v.VoterId)
            .IsRequired()
            .HasColumnType("NVARCHAR(64)");

        builder.Property(v => v.VoteHash)
            .IsRequired()
            .HasColumnType("NVARCHAR(64)");

        builder.Property(v => v.VotedAt)
            .IsRequired();

        // DB-level one-vote-per-voter-per-election enforcement.
        builder.HasIndex(v => new { v.VoterId, v.ElectionId })
            .IsUnique();

        // Restrict (not Cascade) on both FKs: Votes is reachable from Election
        // two ways (Election -> Votes, and Election -> Candidates -> Votes),
        // and SQL Server rejects multiple cascade paths to the same table.
        builder.HasOne(v => v.Election)
            .WithMany()
            .HasForeignKey(v => v.ElectionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Candidate)
            .WithMany()
            .HasForeignKey(v => v.CandidateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
