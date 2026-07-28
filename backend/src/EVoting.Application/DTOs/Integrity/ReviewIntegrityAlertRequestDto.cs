namespace EVoting.Application.DTOs.Integrity;

public class ReviewIntegrityAlertRequestDto
{
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
}
