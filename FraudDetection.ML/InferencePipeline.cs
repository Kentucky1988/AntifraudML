using FraudDetection.Core.Interfaces;
using FraudDetection.Core.Models;

namespace FraudDetection.ML;

/// <summary>
/// End-to-end inference pipeline for fraud detection.
/// Combines feature extraction, anomaly detection, and fraud classification.
/// </summary>
public class InferencePipeline : IInferencePipeline
{
    private readonly IFeatureExtractor _featureExtractor;
    private readonly IAnomalyDetector _anomalyDetector;
    private readonly IFraudClassifier _fraudClassifier;

    /// <summary>
    /// Creates a new inference pipeline with the required components.
    /// </summary>
    /// <param name="featureExtractor">Feature extractor for transaction processing.</param>
    /// <param name="anomalyDetector">Trained anomaly detector (Isolation Forest).</param>
    /// <param name="fraudClassifier">Trained fraud classifier (LightGBM).</param>
    public InferencePipeline(
        IFeatureExtractor featureExtractor,
        IAnomalyDetector anomalyDetector,
        IFraudClassifier fraudClassifier)
    {
        _featureExtractor = featureExtractor ?? throw new ArgumentNullException(nameof(featureExtractor));
        _anomalyDetector = anomalyDetector ?? throw new ArgumentNullException(nameof(anomalyDetector));
        _fraudClassifier = fraudClassifier ?? throw new ArgumentNullException(nameof(fraudClassifier));
    }

    /// <summary>
    /// Predicts fraud for a single transaction through the complete pipeline.
    /// Step 1: Extract features → FeatureVector
    /// Step 2: Anomaly detection → AnomalyScore
    /// Step 3: Classification → FraudPrediction
    /// </summary>
    /// <param name="transaction">Transaction to analyze.</param>
    /// <returns>Fraud prediction with all fields, or null if feature extraction fails.</returns>
    public FraudPrediction? Predict(Transaction transaction)
    {
        if (transaction == null)
            return null;

        // Step 1: Extract features
        var featureVector = _featureExtractor.Extract(transaction);
        if (featureVector == null)
            return null;

        // Step 2: Anomaly detection
        var anomalyResult = _anomalyDetector.Predict(featureVector);

        // Step 3: Classification with combined features
        var classificationInput = new ClassificationInput
        {
            Features = featureVector,
            AnomalyScore = anomalyResult.AnomalyScore
        };

        var prediction = _fraudClassifier.Predict(classificationInput);

        // Ensure all fields are populated
        prediction.TransactionId = transaction.TransactionId;
        prediction.AnomalyScore = anomalyResult.AnomalyScore;

        return prediction;
    }

    /// <summary>
    /// Predicts fraud for multiple transactions through the complete pipeline.
    /// </summary>
    /// <param name="transactions">Array of transactions to analyze.</param>
    /// <returns>Array of fraud predictions (null entries for invalid transactions).</returns>
    public FraudPrediction?[] PredictBatch(Transaction[] transactions)
    {
        if (transactions == null || transactions.Length == 0)
            return Array.Empty<FraudPrediction?>();

        var results = new FraudPrediction?[transactions.Length];

        // Step 1: Extract features for all transactions
        var featureVectors = _featureExtractor.ExtractBatch(transactions);

        // Process each transaction through the pipeline
        for (int i = 0; i < transactions.Length; i++)
        {
            var featureVector = featureVectors[i];
            if (featureVector == null)
            {
                results[i] = null;
                continue;
            }

            // Step 2: Anomaly detection
            var anomalyResult = _anomalyDetector.Predict(featureVector);

            // Step 3: Classification
            var classificationInput = new ClassificationInput
            {
                Features = featureVector,
                AnomalyScore = anomalyResult.AnomalyScore
            };

            var prediction = _fraudClassifier.Predict(classificationInput);
            prediction.TransactionId = transactions[i].TransactionId;
            prediction.AnomalyScore = anomalyResult.AnomalyScore;

            results[i] = prediction;
        }

        return results;
    }
}
