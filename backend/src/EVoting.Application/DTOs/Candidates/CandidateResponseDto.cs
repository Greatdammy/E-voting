namespace EVoting.Application.DTOs.Candidates;

public class CandidateResponseDto
{
    public Guid CandidateId { get; set; }
    public Guid ElectionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Party { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
}
