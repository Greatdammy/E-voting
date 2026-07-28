using EVoting.Application.Common;
using EVoting.Application.DTOs.Integrity;
using EVoting.Domain.Enums;

namespace EVoting.Application.Interfaces;

public interface IIntegrityAlertService
{
    Task<Result<List<IntegrityAlertDto>>> ListAlertsAsync(Guid electionId, IntegrityAlertStatus? status);
    Task<Result<IntegritySummaryDto>> GetSummaryAsync(Guid electionId);
    Task<Result<IntegrityAlertDto>> ReviewAlertAsync(Guid electionId, Guid alertId, ReviewIntegrityAlertRequestDto request, Guid reviewerId);
}
