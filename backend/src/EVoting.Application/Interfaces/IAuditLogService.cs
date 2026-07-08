namespace EVoting.Application.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(Guid? userId, string action);
}
