using EVoting.Application.DTOs.Integrity;
using EVoting.Application.Interfaces;
using EVoting.Domain.Entities;
using EVoting.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EVoting.Infrastructure.Persistence.Repositories;

public class IntegrityAlertRepository : IIntegrityAlertRepository
{
    private readonly AppDbContext _context;

    public IntegrityAlertRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(IntegrityAlert alert)
    {
        await _context.IntegrityAlerts.AddAsync(alert);
    }

    public Task<List<IntegrityAlert>> ListAsync(Guid electionId, IntegrityAlertStatus? status)
    {
        var query = _context.IntegrityAlerts.Where(a => a.ElectionId == electionId);
        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        return query.OrderByDescending(a => a.DetectedAt).ToListAsync();
    }

    public Task<IntegrityAlert?> GetByIdAsync(Guid alertId)
    {
        return _context.IntegrityAlerts.FirstOrDefaultAsync(a => a.AlertId == alertId);
    }

    public async Task<IntegritySummaryDto> GetSummaryAsync(Guid electionId)
    {
        var counts = await _context.IntegrityAlerts
            .Where(a => a.ElectionId == electionId)
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return new IntegritySummaryDto
        {
            ElectionId = electionId,
            OpenCount = counts.FirstOrDefault(c => c.Status == IntegrityAlertStatus.Open)?.Count ?? 0,
            ReviewedCount = counts.FirstOrDefault(c => c.Status == IntegrityAlertStatus.Reviewed)?.Count ?? 0,
            DismissedCount = counts.FirstOrDefault(c => c.Status == IntegrityAlertStatus.Dismissed)?.Count ?? 0
        };
    }
}
