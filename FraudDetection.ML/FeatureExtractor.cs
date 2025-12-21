using FraudDetection.Core.Interfaces;
using FraudDetection.Core.Models;

namespace FraudDetection.ML;

/// <summary>
/// Extracts feature vectors from transactions using counter data.
/// Supports Min-Max normalization to [0,1] range.
/// </summary>
public class FeatureExtractor : IFeatureExtractor
{
    private readonly int _featureCount;
    private float[]? _minValues;
    private float[]? _maxValues;
    private bool _isFitted;

    /// <summary>
    /// Creates a new FeatureExtractor instance.
    /// </summary>
    public FeatureExtractor()
    {
        _featureCount = DepositCounter.TotalCounterCount;
        _isFitted = false;
    }

    /// <summary>
    /// Gets whether the extractor has been fitted with training data for normalization.
    /// </summary>
    public bool IsFitted => _isFitted;

    /// <summary>
    /// Fits the normalizer using training data to compute min/max values per feature.
    /// </summary>
    /// <param name="transactions">Training transactions to compute normalization parameters.</param>
    public void Fit(Transaction[] transactions)
    {
        if (transactions == null || transactions.Length == 0)
        {
            throw new ArgumentException("Training data cannot be null or empty.", nameof(transactions));
        }

        _minValues = new float[_featureCount];
        _maxValues = new float[_featureCount];

        // Initialize with extreme values
        for (int i = 0; i < _featureCount; i++)
        {
            _minValues[i] = float.MaxValue;
            _maxValues[i] = float.MinValue;
        }

        // Compute min/max for each feature
        foreach (var transaction in transactions)
        {
            if (transaction?.Counters == null) continue;

            var features = transaction.Counters.ToFeatureArray();
            for (int i = 0; i < _featureCount; i++)
            {
                if (features[i] < _minValues[i]) _minValues[i] = features[i];
                if (features[i] > _maxValues[i]) _maxValues[i] = features[i];
            }
        }

        // Handle case where all values are the same (avoid division by zero)
        for (int i = 0; i < _featureCount; i++)
        {
            if (_minValues[i] == float.MaxValue) _minValues[i] = 0;
            if (_maxValues[i] == float.MinValue) _maxValues[i] = 0;
        }

        _isFitted = true;
    }

    /// <summary>
    /// Sets normalization parameters directly (useful for loading saved parameters).
    /// </summary>
    /// <param name="minValues">Minimum values per feature.</param>
    /// <param name="maxValues">Maximum values per feature.</param>
    public void SetNormalizationParameters(float[] minValues, float[] maxValues)
    {
        if (minValues == null || maxValues == null)
        {
            throw new ArgumentNullException("Min and max values cannot be null.");
        }

        if (minValues.Length != _featureCount || maxValues.Length != _featureCount)
        {
            throw new ArgumentException($"Arrays must have length {_featureCount}.");
        }

        _minValues = (float[])minValues.Clone();
        _maxValues = (float[])maxValues.Clone();
        _isFitted = true;
    }

    /// <summary>
    /// Gets the current normalization parameters.
    /// </summary>
    /// <returns>Tuple of (minValues, maxValues) arrays, or null if not fitted.</returns>
    public (float[] MinValues, float[] MaxValues)? GetNormalizationParameters()
    {
        if (!_isFitted || _minValues == null || _maxValues == null)
        {
            return null;
        }

        return ((float[])_minValues.Clone(), (float[])_maxValues.Clone());
    }

    /// <summary>
    /// Extracts a feature vector from a single transaction.
    /// Uses TransactionCounters.ToFeatureArray() for extraction.
    /// Applies Min-Max normalization if fitted.
    /// </summary>
    /// <param name="transaction">The transaction to process.</param>
    /// <returns>Feature vector or null if transaction is invalid.</returns>
    public FeatureVector? Extract(Transaction transaction)
    {
        // Validate transaction - return null for invalid transactions (Requirement 1.5)
        if (!IsValidTransaction(transaction))
        {
            return null;
        }

        var features = transaction.Counters.ToFeatureArray();

        // Apply normalization if fitted
        if (_isFitted && _minValues != null && _maxValues != null)
        {
            features = Normalize(features);
        }

        return new FeatureVector
        {
            TransactionId = transaction.TransactionId,
            Features = features
        };
    }

    /// <summary>
    /// Validates a transaction for required fields and data integrity.
    /// </summary>
    /// <param name="transaction">The transaction to validate.</param>
    /// <returns>True if valid, false otherwise.</returns>
    private static bool IsValidTransaction(Transaction? transaction)
    {
        // Null transaction is invalid
        if (transaction == null)
        {
            return false;
        }

        // TransactionId is required (cannot be null or empty)
        if (string.IsNullOrWhiteSpace(transaction.TransactionId))
        {
            return false;
        }

        // Amount must be non-negative
        if (transaction.Amount < 0)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Extracts feature vectors from multiple transactions.
    /// </summary>
    /// <param name="transactions">Array of transactions to process.</param>
    /// <returns>Array of feature vectors (null entries for invalid transactions).</returns>
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

    /// <summary>
    /// Applies Min-Max normalization to feature array.
    /// Formula: (value - min) / (max - min)
    /// Values are clamped to [0, 1] range.
    /// </summary>
    private float[] Normalize(float[] features)
    {
        var normalized = new float[features.Length];

        for (int i = 0; i < features.Length; i++)
        {
            float min = _minValues![i];
            float max = _maxValues![i];
            float range = max - min;

            if (range > 0)
            {
                // Standard Min-Max normalization
                normalized[i] = (features[i] - min) / range;
                // Clamp to [0, 1] for values outside training range
                normalized[i] = Math.Clamp(normalized[i], 0f, 1f);
            }
            else
            {
                // All values are the same, normalize to 0
                normalized[i] = 0f;
            }
        }

        return normalized;
    }
}
