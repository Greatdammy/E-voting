namespace EVoting.Application.DTOs.Elections;

public class BallotCandidateDto
{
    public Guid CandidateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Party { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
}
