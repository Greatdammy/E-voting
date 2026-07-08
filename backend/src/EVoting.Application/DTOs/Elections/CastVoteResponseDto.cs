namespace EVoting.Application.DTOs.Elections;

public class CastVoteResponseDto
{
    public Guid VoteId { get; set; }
    public string ConfirmationHash { get; set; } = string.Empty;
    public DateTime VotedAt { get; set; }
}
