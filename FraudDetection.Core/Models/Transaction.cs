namespace FraudDetection.Core.Models;

/// <summary>
/// Represents a deposit transaction with all associated counters for fraud detection.
/// </summary>
public class Transaction
{
    /// <summary>
    /// Unique transaction identifier.
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;
    
    /// <summary>
    /// Transaction timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Transaction amount in EUR.
    /// </summary>
    public decimal Amount { get; set; }
    
    /// <summary>
    /// User identifier.
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// All counters for fraud analysis (loaded from PostgreSQL JSON).
    /// </summary>
    public TransactionCounters Counters { get; set; } = new();
}
