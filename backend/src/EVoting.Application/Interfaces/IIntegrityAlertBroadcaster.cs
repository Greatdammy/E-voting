using EVoting.Application.DTOs.Integrity;

namespace EVoting.Application.Interfaces;

public interface IIntegrityAlertBroadcaster
{
    Task BroadcastAlertAsync(Guid electionId, IntegrityAlertDto alert);
}
