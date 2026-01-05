namespace FraudDetection.Core.Models;

/// <summary>
/// Vector of normalized features extracted from a transaction.
/// Contains all counters from DepositCounter in a fixed order.
/// </summary>
public class FeatureVector
{
    /// <summary>
    /// Normalized features array. Length equals DepositCounter.TotalCounterCount.
    /// </summary>
    public float[] Features { get; set; } = Array.Empty<float>();
    
    /// <summary>
    /// Associated transaction identifier.
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;
}
