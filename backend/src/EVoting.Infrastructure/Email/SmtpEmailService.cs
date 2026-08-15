using EVoting.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace EVoting.Infrastructure.Email;

/// <summary>
/// Sends OTP emails through a regular SMTP account (e.g. Gmail with an app
/// password) instead of a transactional-email API - useful when signing up
/// for a service like SendGrid isn't an option. Configure via Smtp:Host /
/// Smtp:Port / Smtp:Username / Smtp:Password / Smtp:FromEmail (env vars /
/// user-secrets, never committed).
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _host = configuration["Smtp:Host"]
            ?? throw new InvalidOperationException("Smtp:Host is not configured.");
        _port = int.TryParse(configuration["Smtp:Port"], out var port) ? port : 587;
        _username = configuration["Smtp:Username"]
            ?? throw new InvalidOperationException("Smtp:Username is not configured.");
        _password = configuration["Smtp:Password"]
            ?? throw new InvalidOperationException("Smtp:Password is not configured.");
        // Most providers (Gmail included) require the From address to match
        // the authenticated account, so default to it rather than forcing a
        // separate setting.
        _fromEmail = configuration["Smtp:FromEmail"] ?? _username;
        _fromName = configuration["Smtp:FromName"] ?? "E-Voting";
        _logger = logger;
    }

    public async Task SendOtpEmailAsync(string toEmail, string code, TimeSpan validFor)
    {
        var minutes = (int)Math.Ceiling(validFor.TotalMinutes);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_fromName, _fromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Your voting verification code";
        message.Body = new TextPart("html")
        {
            Text = $"<p>Your verification code is <strong>{code}</strong>.</p>" +
                $"<p>It expires in {minutes} minute(s). If you didn't request this, you can ignore this email.</p>"
        };

        using var client = new SmtpClient();

        try
        {
            // Port 465 is implicit TLS (the connection is TLS from the first
            // byte); 587 and everything else is STARTTLS (connect in the
            // clear, then upgrade). Hardcoding StartTls broke port 465 - some
            // networks block 587 outbound but allow 465, so the option has to
            // track whichever port is actually configured.
            var secureOption = _port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
            await client.ConnectAsync(_host, _port, secureOption);
            await client.AuthenticateAsync(_username, _password);
            await client.SendAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send OTP email via SMTP to {ToEmail}", toEmail);
            throw new InvalidOperationException("Failed to send the verification email.");
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(true);
            }
        }
    }
}