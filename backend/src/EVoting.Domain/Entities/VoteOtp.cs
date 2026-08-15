namespace EVoting.Domain.Entities;

public class VoteOtp
{
    public Guid VoteOtpId { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid ElectionId { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int AttemptCount { get; set; } = 0;
    public bool IsUsed { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public Election? Election { get; set; }
}