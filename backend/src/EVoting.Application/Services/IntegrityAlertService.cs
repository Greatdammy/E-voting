using EVoting.Application.Common;
using EVoting.Application.DTOs.Integrity;
using EVoting.Application.Interfaces;
using EVoting.Domain.Enums;

namespace EVoting.Application.Services;

public class IntegrityAlertService : IIntegrityAlertService
{
    private readonly IElectionRepository _electionRepository;
    private readonly IIntegrityAlertRepository _alertRepository;
    private readonly IUnitOfWork _unitOfWork;

    public IntegrityAlertService(
        IElectionRepository electionRepository,
        IIntegrityAlertRepository alertRepository,
        IUnitOfWork unitOfWork)
    {
        _electionRepository = electionRepository;
        _alertRepository = alertRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<List<IntegrityAlertDto>>> ListAlertsAsync(Guid electionId, IntegrityAlertStatus? status)
    {
        var election = await _electionRepository.GetByIdAsync(electionId);
        if (election is null)
        {
            return Result<List<IntegrityAlertDto>>.Failure(AppError.NotFound, "Election not found.");
        }

        var alerts = await _alertRepository.ListAsync(electionId, status);
        return Result<List<IntegrityAlertDto>>.Success(alerts.Select(IntegrityAlertMapper.ToDto).ToList());
    }

    public async Task<Result<IntegritySummaryDto>> GetSummaryAsync(Guid electionId)
    {
        var election = await _electionRepository.GetByIdAsync(electionId);
        if (election is null)
        {
            return Result<IntegritySummaryDto>.Failure(AppError.NotFound, "Election not found.");
        }

        var summary = await _alertRepository.GetSummaryAsync(electionId);
        return Result<IntegritySummaryDto>.Success(summary);
    }

    public async Task<Result<IntegrityAlertDto>> ReviewAlertAsync(
        Guid electionId, Guid alertId, ReviewIntegrityAlertRequestDto request, Guid reviewerId)
    {
        var alert = await _alertRepository.GetByIdAsync(alertId);
        if (alert is null || alert.ElectionId != electionId)
        {
            return Result<IntegrityAlertDto>.Failure(AppError.NotFound, "Integrity alert not found.");
        }

        alert.Status = Enum.Parse<IntegrityAlertStatus>(request.Status);
        alert.ReviewedBy = reviewerId;
        alert.ReviewedAt = DateTime.UtcNow;
        alert.ReviewNote = request.Note;

        await _unitOfWork.SaveChangesAsync();

        return Result<IntegrityAlertDto>.Success(IntegrityAlertMapper.ToDto(alert));
    }
}
