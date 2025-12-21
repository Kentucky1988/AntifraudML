using FraudDetection.Core.Models;

namespace FraudDetection.Core.Interfaces;

/// <summary>
/// Extracts feature vectors from transactions.
/// </summary>
public interface IFeatureExtractor
{
    /// <summary>
    /// Extracts a feature vector from a single transaction.
    /// </summary>
    /// <param name="transaction">The transaction to process.</param>
    /// <returns>Feature vector or null if transaction is invalid.</returns>
    FeatureVector? Extract(Transaction transaction);
    
    /// <summary>
    /// Extracts feature vectors from multiple transactions.
    /// </summary>
    /// <param name="transactions">Array of transactions to process.</param>
    /// <returns>Array of feature vectors (null entries for invalid transactions).</returns>
    FeatureVector?[] ExtractBatch(Transaction[] transactions);
}
