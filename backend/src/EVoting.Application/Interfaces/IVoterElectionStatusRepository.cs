using EVoting.Domain.Entities;

namespace EVoting.Application.Interfaces;

public interface IVoterElectionStatusRepository
{
    Task<VoterElectionStatus?> GetAsync(Guid userId, Guid electionId);
    Task<HashSet<Guid>> GetVotedElectionIdsAsync(Guid userId);
    Task AddAsync(VoterElectionStatus status);
}
