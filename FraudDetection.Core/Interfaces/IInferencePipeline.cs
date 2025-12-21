using FraudDetection.Core.Models;

namespace FraudDetection.Core.Interfaces;

/// <summary>
/// Inference pipeline for processing new transactions.
/// </summary>
public interface IInferencePipeline
{
    /// <summary>
    /// Predicts fraud for a single transaction.
    /// </summary>
    FraudPrediction? Predict(Transaction transaction);
    
    /// <summary>
    /// Predicts fraud for multiple transactions.
    /// </summary>
    FraudPrediction?[] PredictBatch(Transaction[] transactions);
}
