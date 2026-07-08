using EVoting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVoting.Infrastructure.Persistence.Configurations;

public class CandidateConfiguration : IEntityTypeConfiguration<Candidate>
{
    public void Configure(EntityTypeBuilder<Candidate> builder)
    {
        builder.ToTable("Candidates");

        builder.HasKey(c => c.CandidateId);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.Party)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.PhotoUrl)
            .HasMaxLength(500);

        builder.HasOne(c => c.Election)
            .WithMany()
            .HasForeignKey(c => c.ElectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
