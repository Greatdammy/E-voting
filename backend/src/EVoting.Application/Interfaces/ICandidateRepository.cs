using EVoting.Domain.Entities;

namespace EVoting.Application.Interfaces;

public interface ICandidateRepository
{
    Task<Candidate?> GetByIdAsync(Guid candidateId);
    Task<List<Candidate>> ListByElectionAsync(Guid electionId);
    Task AddAsync(Candidate candidate);
    void Remove(Candidate candidate);
}
