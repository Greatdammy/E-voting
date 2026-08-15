namespace EVoting.Application.Interfaces;

public interface IEmailService
{
    Task SendOtpEmailAsync(string toEmail, string code, TimeSpan validFor);
}