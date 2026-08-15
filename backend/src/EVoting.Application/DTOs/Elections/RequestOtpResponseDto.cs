namespace EVoting.Application.DTOs.Elections;

public class RequestOtpResponseDto
{
    public DateTime ExpiresAt { get; set; }
    public string MaskedEmail { get; set; } = string.Empty;
}