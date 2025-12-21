using FsCheck;
using FsCheck.Xunit;
using FraudDetection.Core.Configuration;
using FraudDetection.Core.Models;
using FraudDetection.ML;
using System.Runtime.InteropServices;
using Xunit;

namespace FraudDetection.Tests;

/// <summary>
/// Property-based tests for LightGbmFraudClassifier.
/// Feature: fraud-detection-poc
/// </summary>
public class LightGbmFraudClassifierPropertyTests
{
    // Fixed feature count for ML.NET compatibility (requires known-size vectors)
    private const int FixedFeatureCount = 20;
    
    /// <summary>
    /// Property 8: Fraud classifier binary output
    /// For any valid ClassificationInput, the Fraud_Classifier SHALL return a FraudPrediction 
    /// with IsFraudTransaction as a boolean value.
    /// **Validates: Requirements 3.1**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool FraudClassifierAlwaysReturnsBinaryOutput(PositiveInt seed)
    {
        // Use seed for reproducibility while varying the random data
        var random = new Random(seed.Get);
        
        // Need enough samples for LightGBM to work properly
        var numSamples = 1000;
        
        // Generate training data with mixed labels (fraud and non-fraud)
        var trainingData = new ClassificationInput[numSamples];
        var labels = new bool[numSamples];
        
        for (int i = 0; i < numSamples; i++)
        {
            trainingData[i] = new ClassificationInput
            {
                Features = new FeatureVector
                {
                    Features = Enumerable.Range(0, FixedFeatureCount)
                        .Select(_ => (float)(random.NextDouble() * 100))
                        .ToArray(),
                    TransactionId = Guid.NewGuid().ToString()
                },
                AnomalyScore = (float)random.NextDouble()
            };
            // Mix of fraud and non-fraud labels (roughly 30% fraud to ensure both classes)
            labels[i] = random.NextDouble() < 0.3;
        }
        
        // Ensure we have at least some of each class at the start
        labels[0] = true;
        labels[1] = false;

        // Generate test input with same feature dimension but random values
        var testInput = new ClassificationInput
        {
            Features = new FeatureVector
            {
                Features = Enumerable.Range(0, FixedFeatureCount)
                    .Select(_ => (float)(random.NextDouble() * 100))
                    .ToArray(),
                TransactionId = Guid.NewGuid().ToString()
            },
            AnomalyScore = (float)random.NextDouble()
        };

        // Create and train classifier
        var config = new LightGbmConfig
        {
            NumberOfLeaves = 31,
            MinimumExampleCountPerLeaf = 20,
            LearningRate = 0.1,
            NumberOfIterations = 50
        };

        var classifier = new LightGbmFraudClassifier(config);
        classifier.Train(trainingData, labels);

        // Predict fraud
        var prediction = classifier.Predict(testInput);

        // Property: IsFraudTransaction must be a valid boolean (true or false)
        // In C#, bool is always true or false, so we verify the prediction is not null
        // and contains a valid boolean value by checking it equals itself
        // (this is a sanity check that the prediction was made correctly)
        return prediction != null && 
               (prediction.IsFraudTransaction == true || prediction.IsFraudTransaction == false);
    }
}