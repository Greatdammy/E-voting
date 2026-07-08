namespace EVoting.Application.DTOs.Elections;

public class BallotResponseDto
{
    public Guid ElectionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<BallotCandidateDto> Candidates { get; set; } = new();
}
