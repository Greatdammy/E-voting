using EVoting.Application.Interfaces;
using EVoting.Domain.Entities;
using EVoting.Domain.Enums;
using EVoting.Infrastructure.Monitoring;
using Moq;
using Xunit;

namespace EVoting.UnitTests.Monitoring;

public class IntegrityMonitoringServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IVoteRepository> _voteRepository = new();
    private readonly Mock<IIntegrityAlertRepository> _alertRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly IntegrityMonitoringService _sut;

    public IntegrityMonitoringServiceTests()
    {
        _alertRepository
            .Setup(r => r.ListAsync(It.IsAny<Guid>(), IntegrityAlertStatus.Open))
            .ReturnsAsync(new List<IntegrityAlert>());

        _sut = new IntegrityMonitoringService(
            _voteRepository.Object,
            _alertRepository.Object,
            _unitOfWork.Object,
            new FixedTimeProvider(Now));
    }

    [Fact]
    public async Task DetectAndPersistAsync_ReturnsEmpty_WhenNoVotesInLookbackWindow()
    {
        var electionId = Guid.NewGuid();
        _voteRepository
            .Setup(r => r.GetVoteTimestampsAsync(electionId, It.IsAny<DateTime>()))
            .ReturnsAsync(new List<DateTime>());

        var result = await _sut.DetectAndPersistAsync(electionId);

        Assert.Empty(result);
        _alertRepository.Verify(r => r.AddAsync(It.IsAny<IntegrityAlert>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DetectAndPersistAsync_RaisesTimingClusterAlert_ForATightBurstOfVotes()
    {
        var electionId = Guid.NewGuid();
        var windowStart = Now.UtcDateTime.AddMinutes(-10);

        // A quiet trickle of one vote every 30 seconds, then a burst of 6
        // votes 0.2s apart — well under the 2-second timing-cluster gap
        // threshold and well over the 5-vote minimum run length.
        var timestamps = new List<DateTime>();
        for (var i = 0; i < 10; i++)
        {
            timestamps.Add(windowStart.AddSeconds(5 + i * 30));
        }

        var burstStart = windowStart.AddSeconds(400);
        for (var i = 0; i < 6; i++)
        {
            timestamps.Add(burstStart.AddMilliseconds(i * 200));
        }

        timestamps.Sort();

        _voteRepository
            .Setup(r => r.GetVoteTimestampsAsync(electionId, It.IsAny<DateTime>()))
            .ReturnsAsync(timestamps);

        var result = await _sut.DetectAndPersistAsync(electionId);

        var timingClusterAlerts = result.Where(a => a.AlertType == nameof(IntegrityAlertType.TimingCluster)).ToList();
        Assert.Single(timingClusterAlerts);
        Assert.Equal(burstStart, timingClusterAlerts[0].WindowStart);
        _alertRepository.Verify(
            r => r.AddAsync(It.Is<IntegrityAlert>(a => a.AlertType == IntegrityAlertType.TimingCluster)),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DetectAndPersistAsync_DoesNotRaiseTimingCluster_WhenRunIsShorterThanMinimumLength()
    {
        var electionId = Guid.NewGuid();
        var windowStart = Now.UtcDateTime.AddMinutes(-10);

        // Only 4 votes in the tight burst — one below the 5-vote minimum.
        var timestamps = Enumerable.Range(0, 4)
            .Select(i => windowStart.AddSeconds(300).AddMilliseconds(i * 200))
            .ToList();

        _voteRepository
            .Setup(r => r.GetVoteTimestampsAsync(electionId, It.IsAny<DateTime>()))
            .ReturnsAsync(timestamps);

        var result = await _sut.DetectAndPersistAsync(electionId);

        Assert.DoesNotContain(result, a => a.AlertType == nameof(IntegrityAlertType.TimingCluster));
    }

    [Fact]
    public async Task DetectAndPersistAsync_SkipsNewAlert_WhenAnOpenAlertAlreadyCoversTheSameWindow()
    {
        var electionId = Guid.NewGuid();
        var windowStart = Now.UtcDateTime.AddMinutes(-10);
        var burstStart = windowStart.AddSeconds(300);

        var timestamps = Enumerable.Range(0, 6)
            .Select(i => burstStart.AddMilliseconds(i * 200))
            .ToList();

        _voteRepository
            .Setup(r => r.GetVoteTimestampsAsync(electionId, It.IsAny<DateTime>()))
            .ReturnsAsync(timestamps);

        _alertRepository
            .Setup(r => r.ListAsync(electionId, IntegrityAlertStatus.Open))
            .ReturnsAsync(new List<IntegrityAlert>
            {
                new()
                {
                    ElectionId = electionId,
                    AlertType = IntegrityAlertType.TimingCluster,
                    WindowStart = burstStart,
                    WindowEnd = timestamps[^1],
                    Status = IntegrityAlertStatus.Open
                }
            });

        var result = await _sut.DetectAndPersistAsync(electionId);

        // The already-open alert only covers the TimingCluster window, so
        // dedup must suppress a second TimingCluster alert for the same
        // burst — but the same burst can independently trip the (different
        // AlertType) velocity-spike detector, which isn't deduped against it.
        Assert.DoesNotContain(result, a => a.AlertType == nameof(IntegrityAlertType.TimingCluster));
        _alertRepository.Verify(
            r => r.AddAsync(It.Is<IntegrityAlert>(a => a.AlertType == IntegrityAlertType.TimingCluster)),
            Times.Never);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(DateTimeOffset now)
        {
            _now = now;
        }

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
