namespace EVoting.Application.DTOs.Integrity;

public class IntegritySummaryDto
{
    public Guid ElectionId { get; set; }
    public int OpenCount { get; set; }
    public int ReviewedCount { get; set; }
    public int DismissedCount { get; set; }
}
