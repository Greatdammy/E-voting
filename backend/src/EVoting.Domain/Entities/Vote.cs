namespace EVoting.Domain.Entities;

public class Vote
{
    public Guid VoteId { get; set; } = Guid.NewGuid();
    public Guid ElectionId { get; set; }
    public string VoterId { get; set; } = string.Empty;
    public Guid CandidateId { get; set; }
    public DateTime VotedAt { get; set; } = DateTime.UtcNow;
    public string VoteHash { get; set; } = string.Empty;

    public Election? Election { get; set; }
    public Candidate? Candidate { get; set; }
}
