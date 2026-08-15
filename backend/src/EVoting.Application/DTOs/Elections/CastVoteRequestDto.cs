namespace EVoting.Application.DTOs.Elections;

public class CastVoteRequestDto
{
    public Guid CandidateId { get; set; }
    public string OtpCode { get; set; } = string.Empty;
}
