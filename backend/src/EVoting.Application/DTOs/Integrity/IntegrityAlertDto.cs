namespace EVoting.Application.DTOs.Integrity;

public class IntegrityAlertDto
{
    public Guid AlertId { get; set; }
    public Guid ElectionId { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public DateTime WindowStart { get; set; }
    public DateTime WindowEnd { get; set; }
    public double ObservedValue { get; set; }
    public double BaselineValue { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
}
