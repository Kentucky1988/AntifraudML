namespace FraudDetection.Core.Configuration;

/// <summary>
/// Configuration for LightGBM fraud classifier.
/// </summary>
public class LightGbmConfig
{
    public int NumberOfLeaves { get; set; } = 31;
    public int MinimumExampleCountPerLeaf { get; set; } = 20;
    public double LearningRate { get; set; } = 0.1;
    public int NumberOfIterations { get; set; } = 100;
}
