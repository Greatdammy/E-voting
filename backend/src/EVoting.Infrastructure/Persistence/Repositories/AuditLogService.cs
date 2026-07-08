using EVoting.Application.Interfaces;
using EVoting.Domain.Entities;

namespace EVoting.Infrastructure.Persistence.Repositories;

public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _context;

    public AuditLogService(AppDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(Guid? userId, string action)
    {
        var entry = new AuditLog
        {
            UserId = userId,
            Action = action
        };

        await _context.AuditLogs.AddAsync(entry);
    }
}
