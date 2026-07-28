namespace EVoting.Infrastructure.Monitoring;

/// <summary>
/// One fixed-width time bucket of vote counts, fed to the ML.NET spike
/// detector as a single point in an IID time series.
/// </summary>
public class VoteCountBucket
{
    public DateTime BucketStart { get; set; }
    public float Count { get; set; }
}
