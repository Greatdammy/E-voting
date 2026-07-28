using EVoting.Application.DTOs.Elections;
using EVoting.Application.Interfaces;
using EVoting.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EVoting.Infrastructure.Persistence.Repositories;

public class VoteRepository : IVoteRepository
{
    private readonly AppDbContext _context;

    public VoteRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Vote vote)
    {
        await _context.Votes.AddAsync(vote);
    }

    public Task<List<CandidateTallyDto>> GetTallyAsync(Guid electionId)
    {
        return _context.Candidates
            .Where(c => c.ElectionId == electionId)
            .Select(c => new CandidateTallyDto
            {
                CandidateId = c.CandidateId,
                Name = c.Name,
                Party = c.Party,
                VoteCount = _context.Votes.Count(v => v.CandidateId == c.CandidateId)
            })
            .ToListAsync();
    }

    public Task<List<DateTime>> GetVoteTimestampsAsync(Guid electionId, DateTime sinceUtc)
    {
        return _context.Votes
            .Where(v => v.ElectionId == electionId && v.VotedAt >= sinceUtc)
            .OrderBy(v => v.VotedAt)
            .Select(v => v.VotedAt)
            .ToListAsync();
    }
}
