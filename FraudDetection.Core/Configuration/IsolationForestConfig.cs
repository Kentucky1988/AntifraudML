namespace FraudDetection.Core.Configuration;

/// <summary>
/// Configuration for Isolation Forest anomaly detector.
/// </summary>
public class IsolationForestConfig
{
    public int NumberOfTrees { get; set; } = 100;
    public int SampleSize { get; set; } = 256;
    public float ContaminationRate { get; set; } = 0.1f;
    public float AnomalyThreshold { get; set; } = 0.5f;
}
