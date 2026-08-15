using EVoting.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace EVoting.Infrastructure.Email;

public class SendGridEmailService : IEmailService
{
    private readonly ISendGridClient _client;
    private readonly string _fromEmail;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(IConfiguration configuration, ILogger<SendGridEmailService> logger)
    {
        var apiKey = configuration["SendGrid:ApiKey"]
            ?? throw new InvalidOperationException("SendGrid:ApiKey is not configured.");
        _fromEmail = configuration["SendGrid:FromEmail"]
            ?? throw new InvalidOperationException("SendGrid:FromEmail is not configured.");

        _client = new SendGridClient(apiKey);
        _logger = logger;
    }

    public async Task SendOtpEmailAsync(string toEmail, string code, TimeSpan validFor)
    {
        var minutes = (int)Math.Ceiling(validFor.TotalMinutes);

        var message = new SendGridMessage
        {
            From = new EmailAddress(_fromEmail, "E-Voting"),
            Subject = "Your voting verification code",
            PlainTextContent = $"Your verification code is {code}. It expires in {minutes} minute(s). " +
                "If you didn't request this, you can ignore this email.",
            HtmlContent = $"<p>Your verification code is <strong>{code}</strong>.</p>" +
                $"<p>It expires in {minutes} minute(s). If you didn't request this, you can ignore this email.</p>"
        };
        message.AddTo(new EmailAddress(toEmail));

        var response = await _client.SendEmailAsync(message);
        if ((int)response.StatusCode >= 400)
        {
            var body = await response.Body.ReadAsStringAsync();
            _logger.LogError("SendGrid failed to send OTP email: {StatusCode} {Body}", response.StatusCode, body);
            throw new InvalidOperationException("Failed to send the verification email.");
        }
    }
}