using EVoting.Application.DTOs.Integrity;
using EVoting.Application.Common;
using EVoting.Application.Interfaces;
using EVoting.Domain.Entities;
using EVoting.Domain.Enums;
using Microsoft.ML;

namespace EVoting.Infrastructure.Monitoring;

/// <summary>
/// Detects statistical anomalies in an election's live voting activity —
/// purely from vote timing and volume, the only signal the anonymised
/// schema can offer without touching IP/device data. This is a triage
/// signal for human review only: it never blocks, rejects, or alters a
/// vote, and every alert it raises must go through Officer/Admin review
/// before anything happens.
/// </summary>
public class IntegrityMonitoringService : IIntegrityMonitoringService
{
    private const int BucketWidthSeconds = 10;
    private const int LookbackMinutes = 10;
    private const int MinimumBucketsForDetection = 30;
    private const int TimingClusterMinRunLength = 5;
    private const double TimingClusterMaxGapSeconds = 2.0;
    private const double CriticalSpikeRatio = 3.0;

    private readonly IVoteRepository _voteRepository;
    private readonly IIntegrityAlertRepository _alertRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public IntegrityMonitoringService(
        IVoteRepository voteRepository,
        IIntegrityAlertRepository alertRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _voteRepository = voteRepository;
        _alertRepository = alertRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<IntegrityAlertDto>> DetectAndPersistAsync(Guid electionId)
    {
        var windowEnd = _timeProvider.GetUtcNow().UtcDateTime;
        var windowStart = windowEnd.AddMinutes(-LookbackMinutes);

        var timestamps = await _voteRepository.GetVoteTimestampsAsync(electionId, windowStart);
        if (timestamps.Count == 0)
        {
            return Array.Empty<IntegrityAlertDto>();
        }

        var openAlerts = await _alertRepository.ListAsync(electionId, IntegrityAlertStatus.Open);

        var candidates = new List<IntegrityAlert>();
        candidates.AddRange(DetectVelocitySpikes(electionId, timestamps, windowStart, windowEnd));
        candidates.AddRange(DetectTimingClusters(electionId, timestamps));

        var newAlerts = candidates
            .Where(candidate => !openAlerts.Any(existing => Overlaps(existing, candidate)))
            .ToList();

        if (newAlerts.Count == 0)
        {
            return Array.Empty<IntegrityAlertDto>();
        }

        foreach (var alert in newAlerts)
        {
            await _alertRepository.AddAsync(alert);
        }

        await _unitOfWork.SaveChangesAsync();

        return newAlerts.Select(IntegrityAlertMapper.ToDto).ToList();
    }

    private static List<IntegrityAlert> DetectVelocitySpikes(
        Guid electionId, List<DateTime> timestamps, DateTime windowStart, DateTime windowEnd)
    {
        var buckets = BuildBuckets(timestamps, windowStart, windowEnd);
        if (buckets.Count < MinimumBucketsForDetection)
        {
            return new List<IntegrityAlert>();
        }

        var mlContext = new MLContext();
        var dataView = mlContext.Data.LoadFromEnumerable(buckets);
        var pipeline = mlContext.Transforms.DetectIidSpike(
            outputColumnName: nameof(SpikePrediction.Prediction),
            inputColumnName: nameof(VoteCountBucket.Count),
            confidence: 95.0,
            pvalueHistoryLength: Math.Max(8, buckets.Count / 4));

        var transformed = pipeline.Fit(dataView).Transform(dataView);
        var predictions = mlContext.Data
            .CreateEnumerable<SpikePrediction>(transformed, reuseRowObject: false)
            .ToList();

        var alerts = new List<IntegrityAlert>();
        for (var i = 0; i < predictions.Count; i++)
        {
            if (predictions[i].Prediction[0] != 1.0)
            {
                continue;
            }

            var baseline = ComputeBaseline(buckets, i);
            var observed = buckets[i].Count;
            var severity = baseline > 0 && observed >= baseline * CriticalSpikeRatio
                ? IntegrityAlertSeverity.Critical
                : IntegrityAlertSeverity.Warning;

            alerts.Add(new IntegrityAlert
            {
                ElectionId = electionId,
                AlertType = IntegrityAlertType.VelocitySpike,
                Severity = severity,
                WindowStart = buckets[i].BucketStart,
                WindowEnd = buckets[i].BucketStart.AddSeconds(BucketWidthSeconds),
                ObservedValue = observed,
                BaselineValue = baseline
            });
        }

        return alerts;
    }

    private static List<IntegrityAlert> DetectTimingClusters(Guid electionId, List<DateTime> timestamps)
    {
        var alerts = new List<IntegrityAlert>();
        if (timestamps.Count < TimingClusterMinRunLength)
        {
            return alerts;
        }

        var overallAverageGap = ComputeAverageGapSeconds(timestamps);
        var runStart = 0;

        for (var i = 1; i <= timestamps.Count; i++)
        {
            var runBroken = i == timestamps.Count ||
                (timestamps[i] - timestamps[i - 1]).TotalSeconds > TimingClusterMaxGapSeconds;

            if (runBroken)
            {
                var runLength = i - runStart;
                if (runLength >= TimingClusterMinRunLength)
                {
                    var runTimestamps = timestamps.GetRange(runStart, runLength);
                    alerts.Add(new IntegrityAlert
                    {
                        ElectionId = electionId,
                        AlertType = IntegrityAlertType.TimingCluster,
                        Severity = runLength >= TimingClusterMinRunLength * 2
                            ? IntegrityAlertSeverity.Critical
                            : IntegrityAlertSeverity.Warning,
                        WindowStart = runTimestamps[0],
                        WindowEnd = runTimestamps[^1],
                        ObservedValue = ComputeAverageGapSeconds(runTimestamps),
                        BaselineValue = overallAverageGap
                    });
                }

                runStart = i;
            }
        }

        return alerts;
    }

    private static bool Overlaps(IntegrityAlert existing, IntegrityAlert candidate) =>
        existing.AlertType == candidate.AlertType &&
        existing.WindowStart < candidate.WindowEnd &&
        candidate.WindowStart < existing.WindowEnd;

    private static double ComputeAverageGapSeconds(List<DateTime> timestamps)
    {
        if (timestamps.Count < 2)
        {
            return 0;
        }

        var totalSeconds = (timestamps[^1] - timestamps[0]).TotalSeconds;
        return totalSeconds / (timestamps.Count - 1);
    }

    private static List<VoteCountBucket> BuildBuckets(List<DateTime> timestamps, DateTime windowStart, DateTime windowEnd)
    {
        var bucketCount = (int)Math.Ceiling((windowEnd - windowStart).TotalSeconds / BucketWidthSeconds);
        var buckets = new List<VoteCountBucket>(bucketCount);

        for (var i = 0; i < bucketCount; i++)
        {
            buckets.Add(new VoteCountBucket
            {
                BucketStart = windowStart.AddSeconds(i * BucketWidthSeconds),
                Count = 0f
            });
        }

        foreach (var timestamp in timestamps)
        {
            var index = (int)((timestamp - windowStart).TotalSeconds / BucketWidthSeconds);
            if (index >= 0 && index < buckets.Count)
            {
                buckets[index].Count += 1f;
            }
        }

        return buckets;
    }

    private static double ComputeBaseline(List<VoteCountBucket> buckets, int spikeIndex)
    {
        var priorCounts = buckets.Take(spikeIndex).Select(b => (double)b.Count).ToList();
        return priorCounts.Count > 0 ? priorCounts.Average() : 0;
    }
}
