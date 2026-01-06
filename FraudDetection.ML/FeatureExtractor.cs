using FraudDetection.Core.Interfaces;
using FraudDetection.Core.Models;

namespace FraudDetection.ML;

/// <summary>
/// Extracts feature vectors from transactions using counter data.
/// Uses FeatureConfiguration to select only relevant features.
/// Normalization is handled by ONNX models (sklearn Pipeline with MinMaxScaler).
/// </summary>
public class FeatureExtractor : IFeatureExtractor
{
    private readonly FeatureConfiguration _config;

    /// <summary>
    /// Gets the feature configuration.
    /// </summary>
    public FeatureConfiguration Configuration => _config;

    /// <summary>
    /// Creates a new FeatureExtractor with optional custom configuration.
    /// </summary>
    /// <param name="configPath">Path to feature_names.json. If null, uses default path.</param>
    public FeatureExtractor(string? configPath = null)
    {
        _config = new FeatureConfiguration(configPath);
    }

    /// <summary>
    /// Extracts a feature vector from a single transaction.
    /// Returns only selected features based on configuration.
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
            Features = _config.ExtractSelectedFeatures(transaction.Counters)
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
