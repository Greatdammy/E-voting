namespace EVoting.Application.DTOs.Candidates;

public class UpdateCandidateRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Party { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
}
