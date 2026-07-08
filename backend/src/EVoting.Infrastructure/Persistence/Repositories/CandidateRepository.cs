using EVoting.Application.Interfaces;
using EVoting.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EVoting.Infrastructure.Persistence.Repositories;

public class CandidateRepository : ICandidateRepository
{
    private readonly AppDbContext _context;

    public CandidateRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Candidate?> GetByIdAsync(Guid candidateId)
    {
        return _context.Candidates.FirstOrDefaultAsync(c => c.CandidateId == candidateId);
    }

    public Task<List<Candidate>> ListByElectionAsync(Guid electionId)
    {
        return _context.Candidates.Where(c => c.ElectionId == electionId).ToListAsync();
    }

    public async Task AddAsync(Candidate candidate)
    {
        await _context.Candidates.AddAsync(candidate);
    }

    public void Remove(Candidate candidate)
    {
        _context.Candidates.Remove(candidate);
    }
}
