using EVoting.Domain.Entities;

namespace EVoting.Application.Interfaces;

public interface IElectionRepository
{
    Task<Election?> GetByIdAsync(Guid electionId);
    Task<List<Election>> ListAsync();
    Task AddAsync(Election election);
}
