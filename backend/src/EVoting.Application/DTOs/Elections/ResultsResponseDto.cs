namespace EVoting.Application.DTOs.Elections;

public class ResultsResponseDto
{
    public Guid ElectionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<CandidateTallyDto> Tally { get; set; } = new();
    public int TotalVotes { get; set; }
}
