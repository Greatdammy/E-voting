using EVoting.Application.DTOs.Elections;
using EVoting.Domain.Entities;

namespace EVoting.Application.Interfaces;

public interface IVoteRepository
{
    Task AddAsync(Vote vote);
    Task<List<CandidateTallyDto>> GetTallyAsync(Guid electionId);
}
