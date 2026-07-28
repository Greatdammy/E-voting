using Microsoft.ML.Data;

namespace EVoting.Infrastructure.Monitoring;

/// <summary>
/// Output row of Microsoft.ML.TimeSeries' DetectIidSpike transform:
/// Prediction[0] is 1.0 when the point is flagged a spike, else 0.0;
/// Prediction[1] is the raw score; Prediction[2] is the p-value.
/// </summary>
public class SpikePrediction
{
    [VectorType(3)]
    public double[] Prediction { get; set; } = Array.Empty<double>();
}
