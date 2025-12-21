namespace FraudDetection.Core.Models;

/// <summary>
/// Input for the LightGBM fraud classifier combining features and anomaly score.
/// </summary>
public class ClassificationInput
{
    public FeatureVector Features { get; set; } = new();
    public float AnomalyScore { get; set; }
}
