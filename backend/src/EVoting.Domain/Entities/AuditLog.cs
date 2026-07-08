namespace EVoting.Domain.Entities;

public class AuditLog
{
    public Guid AuditLogId { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;

    public User? User { get; set; }
}
