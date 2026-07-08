using EVoting.Application.Interfaces;
using EVoting.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EVoting.Infrastructure.Persistence.Repositories;

public class VoterElectionStatusRepository : IVoterElectionStatusRepository
{
    private readonly AppDbContext _context;

    public VoterElectionStatusRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<VoterElectionStatus?> GetAsync(Guid userId, Guid electionId)
    {
        return _context.VoterElectionStatuses
            .FirstOrDefaultAsync(v => v.UserId == userId && v.ElectionId == electionId);
    }

    public async Task<HashSet<Guid>> GetVotedElectionIdsAsync(Guid userId)
    {
        var ids = await _context.VoterElectionStatuses
            .Where(v => v.UserId == userId && v.HasVoted)
            .Select(v => v.ElectionId)
            .ToListAsync();

        return ids.ToHashSet();
    }

    public async Task AddAsync(VoterElectionStatus status)
    {
        await _context.VoterElectionStatuses.AddAsync(status);
    }
}
