using EVoting.Application.Common;
using EVoting.Application.DTOs.Elections;

namespace EVoting.Application.Interfaces;

public interface IVoteService
{
    Task<Result<CastVoteResponseDto>> CastVoteAsync(Guid electionId, Guid voterId, CastVoteRequestDto request);
}
