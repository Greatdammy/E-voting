using EVoting.Application.Interfaces;
using EVoting.Infrastructure.Email;
using EVoting.Infrastructure.Monitoring;
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

        services.AddScoped<IVoteOtpRepository, VoteOtpRepository>();
        services.AddScoped<IOtpCodeHasher, Sha256OtpHasher>();

        // Email sender is picked by whatever's actually configured (env var /
        // user-secrets, per the "no secrets in source" rule) - SMTP first
        // (e.g. Gmail with an app password, no third-party signup needed),
        // then SendGrid if you'd rather use that instead. With neither
        // configured, fall back to logging the code - lets local dev, demos,
        // and the test suite exercise the OTP flow without any real mail
        // account. See LoggingEmailService for the "never in production" caveat.
        if (!string.IsNullOrWhiteSpace(configuration["Smtp:Host"]))
        {
            services.AddScoped<IEmailService, SmtpEmailService>();
        }
        else if (!string.IsNullOrWhiteSpace(configuration["SendGrid:ApiKey"]))
        {
            services.AddScoped<IEmailService, SendGridEmailService>();
        }
        else
        {
            services.AddScoped<IEmailService, LoggingEmailService>();
        }

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IIntegrityAlertRepository, IntegrityAlertRepository>();
        services.AddScoped<IIntegrityMonitoringService, IntegrityMonitoringService>();

        return services;
    }
}
