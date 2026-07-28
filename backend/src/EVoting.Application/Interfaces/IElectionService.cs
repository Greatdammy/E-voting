using EVoting.Application.Common;
using EVoting.Application.DTOs.Elections;

namespace EVoting.Application.Interfaces;

public interface IElectionService
{
    Task<Result<ElectionResponseDto>> CreateElectionAsync(CreateElectionRequestDto request, Guid createdBy);
    Task<Result<List<ElectionResponseDto>>> ListElectionsAsync();
    Task<Result<List<ElectionSummaryDto>>> GetElectionsForVoterAsync(Guid voterId);
    Task<Result<BallotResponseDto>> GetBallotAsync(Guid electionId, Guid voterId);
    Task<Result<ResultsResponseDto>> GetResultsAsync(Guid electionId);
    Task<Result<bool>> DeleteElectionAsync(Guid electionId, Guid deletedBy);
}
