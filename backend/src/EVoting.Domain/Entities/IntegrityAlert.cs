using EVoting.Domain.Enums;

namespace EVoting.Domain.Entities;

public class IntegrityAlert
{
    public Guid AlertId { get; set; } = Guid.NewGuid();
    public Guid ElectionId { get; set; }
    public IntegrityAlertType AlertType { get; set; }
    public IntegrityAlertSeverity Severity { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public DateTime WindowStart { get; set; }
    public DateTime WindowEnd { get; set; }
    public double ObservedValue { get; set; }
    public double BaselineValue { get; set; }
    public IntegrityAlertStatus Status { get; set; } = IntegrityAlertStatus.Open;
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }

    public Election? Election { get; set; }
    public User? ReviewedByUser { get; set; }
}
