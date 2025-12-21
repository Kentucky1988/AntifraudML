using FsCheck;
using FsCheck.Xunit;
using FraudDetection.Core.Configuration;
using FraudDetection.Core.Models;
using FraudDetection.ML;

namespace FraudDetection.Tests;

/// <summary>
/// Property-based tests for IsolationForestDetector.
/// Feature: fraud-detection-poc
/// </summary>
public class IsolationForestPropertyTests
{
    /// <summary>
    /// Property 5: Anomaly score range
    /// For any FeatureVector, the Anomaly_Detector SHALL return an AnomalyScore in the range [0, 1].
    /// **Validates: Requirements 2.1**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool AnomalyScoreIsAlwaysInZeroToOneRange(PositiveInt featureCount, PositiveInt sampleCount)
    {
        // Constrain to reasonable sizes for test performance
        var numFeatures = Math.Min(featureCount.Get, 50);
        var numSamples = Math.Min(Math.Max(sampleCount.Get, 2), 100);
        
        var random = new Random();
        
        // Generate training data
        var trainingData = Enumerable.Range(0, numSamples)
            .Select(_ => new FeatureVector
            {
                Features = Enumerable.Range(0, numFeatures)
                    .Select(_ => (float)(random.NextDouble() * 100))
                    .ToArray(),
                TransactionId = Guid.NewGuid().ToString()
            })
            .ToArray();

        // Generate test vector with same dimension
        var testVector = new FeatureVector
        {
            Features = Enumerable.Range(0, numFeatures)
                .Select(_ => (float)(random.NextDouble() * 100))
                .ToArray(),
            TransactionId = Guid.NewGuid().ToString()
        };

        // Create and train detector with small config for faster tests
        var config = new IsolationForestConfig
        {
            NumberOfTrees = 10,
            SampleSize = Math.Min(50, numSamples),
            AnomalyThreshold = 0.5f
        };

        var detector = new IsolationForestDetector(config);
        detector.Train(trainingData);

        // Predict anomaly score
        var result = detector.Predict(testVector);

        // Property: AnomalyScore must be in [0, 1]
        return result.AnomalyScore >= 0f && result.AnomalyScore <= 1f;
    }

    /// <summary>
    /// Property 6: Anomaly threshold consistency
    /// For any AnomalyResult, IsAnomaly SHALL be true if and only if AnomalyScore >= configured threshold.
    /// **Validates: Requirements 2.3**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool IsAnomalyFlagIsConsistentWithThreshold(PositiveInt featureCount, PositiveInt sampleCount)
    {
        // Constrain to reasonable sizes for test performance
        var numFeatures = Math.Min(featureCount.Get, 50);
        var numSamples = Math.Min(Math.Max(sampleCount.Get, 2), 100);
        
        var random = new Random();
        
        // Generate training data
        var trainingData = Enumerable.Range(0, numSamples)
            .Select(_ => new FeatureVector
            {
                Features = Enumerable.Range(0, numFeatures)
                    .Select(_ => (float)(random.NextDouble() * 100))
                    .ToArray(),
                TransactionId = Guid.NewGuid().ToString()
            })
            .ToArray();

        // Generate test vector with same dimension
        var testVector = new FeatureVector
        {
            Features = Enumerable.Range(0, numFeatures)
                .Select(_ => (float)(random.NextDouble() * 100))
                .ToArray(),
            TransactionId = Guid.NewGuid().ToString()
        };

        // Use a random threshold between 0 and 1
        var threshold = (float)(random.NextDouble());

        // Create and train detector with the random threshold
        var config = new IsolationForestConfig
        {
            NumberOfTrees = 10,
            SampleSize = Math.Min(50, numSamples),
            AnomalyThreshold = threshold
        };

        var detector = new IsolationForestDetector(config);
        detector.Train(trainingData);

        // Predict anomaly score
        var result = detector.Predict(testVector);

        // Property: IsAnomaly must be true iff AnomalyScore >= threshold
        var expectedIsAnomaly = result.AnomalyScore >= threshold;
        return result.IsAnomaly == expectedIsAnomaly;
    }

    /// <summary>
    /// Property 7: Anomaly detector model round-trip
    /// For any trained Anomaly_Detector model, saving to disk and loading back SHALL produce identical predictions for the same input.
    /// **Validates: Requirements 2.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ModelRoundTripProducesIdenticalPredictions(PositiveInt featureCount, PositiveInt sampleCount, PositiveInt testCount)
    {
        // Constrain to reasonable sizes for test performance
        var numFeatures = Math.Min(featureCount.Get, 50);
        var numSamples = Math.Min(Math.Max(sampleCount.Get, 2), 100);
        var numTestVectors = Math.Min(Math.Max(testCount.Get, 1), 20);
        
        var random = new Random(42); // Fixed seed for reproducibility within test
        
        // Generate training data
        var trainingData = Enumerable.Range(0, numSamples)
            .Select(_ => new FeatureVector
            {
                Features = Enumerable.Range(0, numFeatures)
                    .Select(_ => (float)(random.NextDouble() * 100))
                    .ToArray(),
                TransactionId = Guid.NewGuid().ToString()
            })
            .ToArray();

        // Generate test vectors with same dimension
        var testVectors = Enumerable.Range(0, numTestVectors)
            .Select(i => new FeatureVector
            {
                Features = Enumerable.Range(0, numFeatures)
                    .Select(_ => (float)(random.NextDouble() * 100))
                    .ToArray(),
                TransactionId = $"test-{i}"
            })
            .ToArray();

        // Create and train original detector
        var config = new IsolationForestConfig
        {
            NumberOfTrees = 10,
            SampleSize = Math.Min(50, numSamples),
            AnomalyThreshold = 0.5f
        };

        var originalDetector = new IsolationForestDetector(config);
        originalDetector.Train(trainingData);

        // Get predictions from original model
        var originalPredictions = testVectors
            .Select(v => originalDetector.Predict(v))
            .ToArray();

        // Save model to temp file
        var tempPath = Path.Combine(Path.GetTempPath(), $"isolation_forest_test_{Guid.NewGuid()}.json");
        
        try
        {
            originalDetector.SaveModel(tempPath);

            // Load model into new detector
            var loadedDetector = new IsolationForestDetector(config);
            loadedDetector.LoadModel(tempPath);

            // Get predictions from loaded model
            var loadedPredictions = testVectors
                .Select(v => loadedDetector.Predict(v))
                .ToArray();

            // Property: All predictions must be identical
            for (int i = 0; i < testVectors.Length; i++)
            {
                // Compare AnomalyScore with tolerance for floating point
                if (Math.Abs(originalPredictions[i].AnomalyScore - loadedPredictions[i].AnomalyScore) > 1e-6f)
                    return false;

                // Compare IsAnomaly flag
                if (originalPredictions[i].IsAnomaly != loadedPredictions[i].IsAnomaly)
                    return false;

                // Compare TransactionId
                if (originalPredictions[i].TransactionId != loadedPredictions[i].TransactionId)
                    return false;
            }

            return true;
        }
        finally
        {
            // Cleanup temp file
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}
