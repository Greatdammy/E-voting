using EVoting.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EVoting.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Election> Elections => Set<Election>();
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<Vote> Votes => Set<Vote>();
    public DbSet<VoterElectionStatus> VoterElectionStatuses => Set<VoterElectionStatus>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
