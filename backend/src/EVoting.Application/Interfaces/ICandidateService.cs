using EVoting.Application.Common;
using EVoting.Application.DTOs.Candidates;

namespace EVoting.Application.Interfaces;

public interface ICandidateService
{
    Task<Result<List<CandidateResponseDto>>> ListCandidatesAsync(Guid electionId);
    Task<Result<CandidateResponseDto>> CreateCandidateAsync(Guid electionId, CreateCandidateRequestDto request);
    Task<Result<CandidateResponseDto>> UpdateCandidateAsync(Guid electionId, Guid candidateId, UpdateCandidateRequestDto request);
    Task<Result<bool>> DeleteCandidateAsync(Guid electionId, Guid candidateId);
}
