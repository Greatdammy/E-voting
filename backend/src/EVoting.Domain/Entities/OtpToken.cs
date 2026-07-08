namespace EVoting.Domain.Entities;

public class OtpToken
{
    public Guid OtpTokenId { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string OtpHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
