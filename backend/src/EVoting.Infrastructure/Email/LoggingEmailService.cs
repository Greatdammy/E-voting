using EVoting.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace EVoting.Infrastructure.Email;

/// <summary>
/// Fallback used when neither Smtp:Host nor SendGrid:ApiKey is configured, so
/// local development, demos, and the automated test suite can exercise the
/// full OTP flow without any real mail account. Never wired up once one of
/// those is configured - see DependencyInjection.AddInfrastructure.
/// </summary>
public class LoggingEmailService : IEmailService
{
    private readonly ILogger<LoggingEmailService> _logger;

    public LoggingEmailService(ILogger<LoggingEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendOtpEmailAsync(string toEmail, string code, TimeSpan validFor)
    {
        _logger.LogWarning(
            "[DEV MODE - SendGrid not configured] OTP for {ToEmail} is {Code}, valid for {ValidForMinutes} minute(s). " +
            "This is logged only because SendGrid:ApiKey is empty - never rely on this in production.",
            toEmail, code, (int)Math.Ceiling(validFor.TotalMinutes));

        return Task.CompletedTask;
    }
}