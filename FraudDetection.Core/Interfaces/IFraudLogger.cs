using FraudDetection.Core.Models;

namespace FraudDetection.Core.Interfaces;

/// <summary>
/// Logger interface for fraud detection system.
/// Supports structured JSON logging for transactions, errors, and latency metrics.
/// </summary>
public interface IFraudLogger
{
    /// <summary>
    /// Logs a processed transaction with its fraud detection results.
    /// </summary>
    /// <param name="transactionId">The unique identifier of the transaction</param>
    /// <param name="anomalyScore">The anomaly score from Isolation Forest (0-1)</param>
    /// <param name="isFraud">Whether the transaction was classified as fraud</param>
    /// <param name="latencyMs">Processing latency in milliseconds</param>
    void LogTransaction(string transactionId, float anomalyScore, bool isFraud, double latencyMs);

    /// <summary>
    /// Logs an error with context information.
    /// </summary>
    /// <param name="exception">The exception that occurred</param>
    /// <param name="context">Additional context about where/why the error occurred</param>
    void LogError(Exception exception, string context);

    /// <summary>
    /// Logs latency metrics for a specific operation.
    /// </summary>
    /// <param name="operationName">Name of the operation (e.g., "FeatureExtraction", "AnomalyDetection")</param>
    /// <param name="latencyMs">Operation latency in milliseconds</param>
    void LogLatency(string operationName, double latencyMs);
}
