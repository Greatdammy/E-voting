using EVoting.Application.DTOs.Integrity;

namespace EVoting.Application.Interfaces;

public interface IIntegrityMonitoringService
{
    Task<IReadOnlyList<IntegrityAlertDto>> DetectAndPersistAsync(Guid electionId);
}
