using FraudDetection.Core.Models;

namespace FraudDetection.Core.Interfaces;

/// <summary>
/// Training pipeline for both anomaly detector and fraud classifier.
/// </summary>
public interface ITrainingPipeline
{
    /// <summary>
    /// Trains both models on the provided data.
    /// </summary>
    TrainingResult Train(TrainingData data);
    
    /// <summary>
    /// Evaluates the trained models on test data.
    /// </summary>
    ModelMetrics Evaluate(TrainingData testData);
}
