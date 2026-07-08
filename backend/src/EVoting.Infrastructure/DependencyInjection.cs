using EVoting.Application.Interfaces;
using EVoting.Infrastructure.Persistence;
using EVoting.Infrastructure.Persistence.Repositories;
using EVoting.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EVoting.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Default")));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        services.AddScoped<IElectionRepository, ElectionRepository>();
        services.AddScoped<ICandidateRepository, CandidateRepository>();
        services.AddScoped<IVoteRepository, VoteRepository>();
        services.AddScoped<IVoterElectionStatusRepository, VoterElectionStatusRepository>();
        services.AddScoped<IVoterAnonymizer, Sha256VoterAnonymizer>();
        services.AddScoped<IConfirmationHashService, ConfirmationHashService>();

        return services;
    }
}
