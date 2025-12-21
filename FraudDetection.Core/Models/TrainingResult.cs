namespace FraudDetection.Core.Models;

/// <summary>
/// Result from the training pipeline.
/// </summary>
public class TrainingResult
{
    public bool Success { get; set; }
    public ModelMetrics Metrics { get; set; } = new();
    public string IsolationForestModelPath { get; set; } = string.Empty;
    public string LightGbmModelPath { get; set; } = string.Empty;
}
