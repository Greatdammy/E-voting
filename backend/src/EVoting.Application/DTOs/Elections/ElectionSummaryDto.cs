namespace EVoting.Application.DTOs.Elections;

public class ElectionSummaryDto
{
    public Guid ElectionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool HasVoted { get; set; }
}
