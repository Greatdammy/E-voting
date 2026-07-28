using EVoting.Application.Interfaces;
using EVoting.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EVoting.Infrastructure.Persistence.Repositories;

public class ElectionRepository : IElectionRepository
{
    private readonly AppDbContext _context;

    public ElectionRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Election?> GetByIdAsync(Guid electionId)
    {
        return _context.Elections.FirstOrDefaultAsync(e => e.ElectionId == electionId);
    }

    public Task<List<Election>> ListAsync()
    {
        return _context.Elections.OrderByDescending(e => e.StartDate).ToListAsync();
    }

    public async Task AddAsync(Election election)
    {
        await _context.Elections.AddAsync(election);
    }

    public void Remove(Election election)
    {
        _context.Elections.Remove(election);
    }
}
