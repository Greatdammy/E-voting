using EVoting.Application.Common;
using EVoting.Application.Interfaces;
using EVoting.Domain.Enums;

namespace EVoting.API.BackgroundServices;

/// <summary>
/// Polls every Active election on a fixed interval and runs anomaly
/// detection over its recent voting activity. Deliberately kept out of the
/// vote-cast transaction path in VoteService — integrity detection must
/// never add latency or risk to the security-critical vote write.
/// </summary>
public class IntegrityMonitorBackgroundService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(20);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IntegrityMonitorBackgroundService> _logger;

    public IntegrityMonitorBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<IntegrityMonitorBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunDetectionPassAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Integrity monitoring pass failed.");
            }
        }
    }

    private async Task RunDetectionPassAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var electionRepository = scope.ServiceProvider.GetRequiredService<IElectionRepository>();
        var monitoringService = scope.ServiceProvider.GetRequiredService<IIntegrityMonitoringService>();
        var broadcaster = scope.ServiceProvider.GetRequiredService<IIntegrityAlertBroadcaster>();

        var elections = await electionRepository.ListAsync();
        var now = DateTime.UtcNow;
        var activeElectionIds = elections
            .Where(e => ElectionStatusCalculator.Compute(e.StartDate, e.EndDate, now) == ElectionStatus.Active)
            .Select(e => e.ElectionId)
            .ToList();

        foreach (var electionId in activeElectionIds)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var newAlerts = await monitoringService.DetectAndPersistAsync(electionId);
            foreach (var alert in newAlerts)
            {
                await broadcaster.BroadcastAlertAsync(electionId, alert);
            }
        }
    }
}
