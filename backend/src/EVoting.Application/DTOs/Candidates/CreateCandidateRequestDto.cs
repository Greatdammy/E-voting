namespace EVoting.Application.DTOs.Candidates;

public class CreateCandidateRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Party { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
}
