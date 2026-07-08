namespace EVoting.Domain.Entities;

public class VoterElectionStatus
{
    public Guid UserId { get; set; }
    public Guid ElectionId { get; set; }
    public bool HasVoted { get; set; } = false;
    public DateTime? VotedAt { get; set; }

    public User? User { get; set; }
    public Election? Election { get; set; }
}
