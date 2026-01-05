using FraudDetection.Core.Interfaces;
using FraudDetection.Core.Models;

namespace FraudDetection.ML;

/// <summary>
/// Extracts feature vectors from transactions using counter data.
/// Normalization is handled by ONNX models (sklearn Pipeline with MinMaxScaler).
/// </summary>
public class FeatureExtractor : IFeatureExtractor
{
    /// <summary>
    /// Extracts a feature vector from a single transaction.
    /// Returns raw (unnormalized) features - normalization is done in ONNX model.
    /// </summary>
    public FeatureVector? Extract(Transaction transaction)
    {
        if (!IsValidTransaction(transaction))
        {
            return null;
        }

        return new FeatureVector
        {
            TransactionId = transaction.TransactionId,
            Features = transaction.Counters.ToCountersArray()
        };
    }

    /// <summary>
    /// Extracts feature vectors from multiple transactions.
    /// </summary>
    public FeatureVector?[] ExtractBatch(Transaction[] transactions)
    {
        if (transactions == null)
        {
            return Array.Empty<FeatureVector?>();
        }

        var results = new FeatureVector?[transactions.Length];
        
        for (int i = 0; i < transactions.Length; i++)
        {
            results[i] = Extract(transactions[i]);
        }

        return results;
    }

    private static bool IsValidTransaction(Transaction? transaction)
    {
        if (transaction == null)
            return false;

        if (string.IsNullOrWhiteSpace(transaction.TransactionId))
            return false;

        if (transaction.Amount < 0)
            return false;

        return true;
    }
}
