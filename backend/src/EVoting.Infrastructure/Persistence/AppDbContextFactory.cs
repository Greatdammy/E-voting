using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EVoting.Infrastructure.Persistence;

/// <summary>
/// Lets `dotnet ef migrations add` build the model without a real database —
/// migration generation never opens a connection, so this placeholder
/// connection string is only used to satisfy UseSqlServer's design-time
/// requirements. The real connection string (env var / user-secrets) is
/// wired in EVoting.API's Program.cs for runtime use.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer("Server=.;Database=EVoting;Trusted_Connection=True;TrustServerCertificate=True;");

        return new AppDbContext(optionsBuilder.Options);
    }
}
