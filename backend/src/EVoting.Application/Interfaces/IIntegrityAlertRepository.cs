using EVoting.Application.DTOs.Integrity;
using EVoting.Domain.Entities;
using EVoting.Domain.Enums;

namespace EVoting.Application.Interfaces;

public interface IIntegrityAlertRepository
{
    Task AddAsync(IntegrityAlert alert);
    Task<List<IntegrityAlert>> ListAsync(Guid electionId, IntegrityAlertStatus? status);
    Task<IntegrityAlert?> GetByIdAsync(Guid alertId);
    Task<IntegritySummaryDto> GetSummaryAsync(Guid electionId);
}
