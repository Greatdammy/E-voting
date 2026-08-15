using EVoting.Application.Interfaces;
using EVoting.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EVoting.Infrastructure.Persistence.Repositories;

public class VoteOtpRepository : IVoteOtpRepository
{
    private readonly AppDbContext _context;

    public VoteOtpRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<VoteOtp?> GetLatestAsync(Guid userId, Guid electionId)
    {
        return _context.VoteOtps
            .Where(o => o.UserId == userId && o.ElectionId == electionId)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public Task<int> CountRequestsSinceAsync(Guid userId, Guid electionId, DateTime sinceUtc)
    {
        return _context.VoteOtps
            .CountAsync(o => o.UserId == userId && o.ElectionId == electionId && o.CreatedAt >= sinceUtc);
    }

    public async Task AddAsync(VoteOtp otp)
    {
        await _context.VoteOtps.AddAsync(otp);
    }
}