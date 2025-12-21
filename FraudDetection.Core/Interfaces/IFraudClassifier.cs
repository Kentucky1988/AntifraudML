using FraudDetection.Core.Models;

namespace FraudDetection.Core.Interfaces;

/// <summary>
/// LightGBM based fraud classifier.
/// </summary>
public interface IFraudClassifier
{
    /// <summary>
    /// Trains the classifier on labeled data.
    /// </summary>
    void Train(ClassificationInput[] trainingData, bool[] labels);
    
    /// <summary>
    /// Predicts fraud for a single input.
    /// </summary>
    FraudPrediction Predict(ClassificationInput input);
    
    /// <summary>
    /// Predicts fraud for multiple inputs.
    /// </summary>
    FraudPrediction[] PredictBatch(ClassificationInput[] inputs);
    
    /// <summary>
    /// Saves the trained model to disk.
    /// </summary>
    void SaveModel(string path);
    
    /// <summary>
    /// Loads a trained model from disk.
    /// </summary>
    void LoadModel(string path);
}
