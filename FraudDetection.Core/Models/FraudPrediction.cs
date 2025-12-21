namespace FraudDetection.Core.Models;

/// <summary>
/// Final fraud prediction result from the classification pipeline.
/// </summary>
public class FraudPrediction
{
    public string TransactionId { get; set; } = string.Empty;
    
    /// <summary>
    /// Final fraud classification result.
    /// </summary>
    public bool IsFraudTransaction { get; set; }
    
    /// <summary>
    /// Confidence score between 0 and 1.
    /// </summary>
    public float ConfidenceScore { get; set; }
    
    /// <summary>
    /// Anomaly score from the Isolation Forest stage.
    /// </summary>
    public float AnomalyScore { get; set; }
}
