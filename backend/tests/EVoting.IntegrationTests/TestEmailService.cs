using System.Collections.Concurrent;
using EVoting.Application.Interfaces;

namespace EVoting.IntegrationTests;

/// <summary>
/// Replaces the real IEmailService in the test host so integration tests can
/// read back the OTP that "would have been" emailed, without ever going
/// through SendGrid or the LoggingEmailService console fallback.
/// </summary>
public class TestEmailService : IEmailService
{
    private readonly ConcurrentDictionary<string, string> _lastCodeByEmail = new();

    public Task SendOtpEmailAsync(string toEmail, string code, TimeSpan validFor)
    {
        _lastCodeByEmail[toEmail] = code;
        return Task.CompletedTask;
    }

    public string GetLastCode(string toEmail) => _lastCodeByEmail[toEmail];
}