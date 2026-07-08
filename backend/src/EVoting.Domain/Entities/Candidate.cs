namespace EVoting.Domain.Entities;

public class Candidate
{
    public Guid CandidateId { get; set; } = Guid.NewGuid();
    public Guid ElectionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Party { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }

    public Election? Election { get; set; }
}
