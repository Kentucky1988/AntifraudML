using FraudDetection.Core.Models;

namespace FraudDetection.Core.Interfaces;

/// <summary>
/// Isolation Forest based anomaly detector.
/// </summary>
public interface IAnomalyDetector
{
    /// <summary>
    /// Trains the anomaly detector on historical data.
    /// </summary>
    void Train(FeatureVector[] trainingData);
    
    /// <summary>
    /// Predicts anomaly score for a single feature vector.
    /// </summary>
    AnomalyResult Predict(FeatureVector features);
    
    /// <summary>
    /// Predicts anomaly scores for multiple feature vectors.
    /// </summary>
    AnomalyResult[] PredictBatch(FeatureVector[] features);
    
    /// <summary>
    /// Saves the trained model to disk.
    /// </summary>
    void SaveModel(string path);
    
    /// <summary>
    /// Loads a trained model from disk.
    /// </summary>
    void LoadModel(string path);
}
