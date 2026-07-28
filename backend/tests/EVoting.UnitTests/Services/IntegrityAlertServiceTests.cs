using EVoting.Application.Common;
using EVoting.Application.DTOs.Integrity;
using EVoting.Application.Interfaces;
using EVoting.Application.Services;
using EVoting.Domain.Entities;
using EVoting.Domain.Enums;
using Moq;
using Xunit;

namespace EVoting.UnitTests.Services;

public class IntegrityAlertServiceTests
{
    private readonly Mock<IElectionRepository> _electionRepository = new();
    private readonly Mock<IIntegrityAlertRepository> _alertRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly IntegrityAlertService _sut;

    public IntegrityAlertServiceTests()
    {
        _sut = new IntegrityAlertService(_electionRepository.Object, _alertRepository.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task ListAlertsAsync_ReturnsNotFound_WhenElectionDoesNotExist()
    {
        var electionId = Guid.NewGuid();
        _electionRepository.Setup(r => r.GetByIdAsync(electionId)).ReturnsAsync((Election?)null);

        var result = await _sut.ListAlertsAsync(electionId, status: null);

        Assert.False(result.Succeeded);
        Assert.Equal(AppError.NotFound, result.Error);
    }

    [Fact]
    public async Task ListAlertsAsync_ReturnsMappedAlerts_WhenElectionExists()
    {
        var election = new Election();
        var alert = new IntegrityAlert
        {
            ElectionId = election.ElectionId,
            AlertType = IntegrityAlertType.VelocitySpike,
            Severity = IntegrityAlertSeverity.Warning,
            Status = IntegrityAlertStatus.Open
        };

        _electionRepository.Setup(r => r.GetByIdAsync(election.ElectionId)).ReturnsAsync(election);
        _alertRepository
            .Setup(r => r.ListAsync(election.ElectionId, IntegrityAlertStatus.Open))
            .ReturnsAsync(new List<IntegrityAlert> { alert });

        var result = await _sut.ListAlertsAsync(election.ElectionId, IntegrityAlertStatus.Open);

        Assert.True(result.Succeeded);
        Assert.Single(result.Value!);
        Assert.Equal(alert.AlertId, result.Value![0].AlertId);
        Assert.Equal(nameof(IntegrityAlertType.VelocitySpike), result.Value![0].AlertType);
    }

    [Fact]
    public async Task ReviewAlertAsync_ReturnsNotFound_WhenAlertBelongsToDifferentElection()
    {
        var alert = new IntegrityAlert { ElectionId = Guid.NewGuid() };
        _alertRepository.Setup(r => r.GetByIdAsync(alert.AlertId)).ReturnsAsync(alert);

        var result = await _sut.ReviewAlertAsync(
            Guid.NewGuid(), alert.AlertId, new ReviewIntegrityAlertRequestDto { Status = "Dismissed" }, Guid.NewGuid());

        Assert.False(result.Succeeded);
        Assert.Equal(AppError.NotFound, result.Error);
    }

    [Fact]
    public async Task ReviewAlertAsync_UpdatesAlertStatusAndReviewer_WhenAlertExists()
    {
        var alert = new IntegrityAlert { Status = IntegrityAlertStatus.Open };
        var reviewerId = Guid.NewGuid();
        _alertRepository.Setup(r => r.GetByIdAsync(alert.AlertId)).ReturnsAsync(alert);

        var result = await _sut.ReviewAlertAsync(
            alert.ElectionId,
            alert.AlertId,
            new ReviewIntegrityAlertRequestDto { Status = "Reviewed", Note = "Confirmed a known bulk-voting kiosk." },
            reviewerId);

        Assert.True(result.Succeeded);
        Assert.Equal(nameof(IntegrityAlertStatus.Reviewed), result.Value!.Status);
        Assert.Equal(IntegrityAlertStatus.Reviewed, alert.Status);
        Assert.Equal(reviewerId, alert.ReviewedBy);
        Assert.NotNull(alert.ReviewedAt);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}
