namespace FraudDetection.Core.Models;

/// <summary>
/// Result from the Isolation Forest anomaly detection.
/// </summary>
public class AnomalyResult
{
    public string TransactionId { get; set; } = string.Empty;
    
    /// <summary>
    /// Anomaly score between 0 and 1. Higher values indicate more anomalous.
    /// </summary>
    public float AnomalyScore { get; set; }
    
    /// <summary>
    /// Whether the transaction is flagged as anomalous based on threshold.
    /// </summary>
    public bool IsAnomaly { get; set; }
}
