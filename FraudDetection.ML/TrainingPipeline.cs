using FraudDetection.Core.Interfaces;
using FraudDetection.Core.Models;

namespace FraudDetection.ML;

/// <summary>
/// Training pipeline that orchestrates the training of both Isolation Forest
/// and LightGBM models in sequence.
/// </summary>
public class TrainingPipeline : ITrainingPipeline
{
    private readonly IFeatureExtractor _featureExtractor;
    private readonly IAnomalyDetector _anomalyDetector;
    private readonly IFraudClassifier _fraudClassifier;
    
    private string _isolationForestModelPath = "isolation_forest.json";
    private string _lightGbmModelPath = "lightgbm.zip";

    /// <summary>
    /// Creates a new TrainingPipeline with the specified components.
    /// </summary>
    /// <param name="featureExtractor">Feature extractor for processing transactions.</param>
    /// <param name="anomalyDetector">Isolation Forest anomaly detector.</param>
    /// <param name="fraudClassifier">LightGBM fraud classifier.</param>
    public TrainingPipeline(
        IFeatureExtractor featureExtractor,
        IAnomalyDetector anomalyDetector,
        IFraudClassifier fraudClassifier)
    {
        _featureExtractor = featureExtractor ?? throw new ArgumentNullException(nameof(featureExtractor));
        _anomalyDetector = anomalyDetector ?? throw new ArgumentNullException(nameof(anomalyDetector));
        _fraudClassifier = fraudClassifier ?? throw new ArgumentNullException(nameof(fraudClassifier));
    }

    /// <summary>
    /// Sets the model output paths.
    /// </summary>
    /// <param name="isolationForestPath">Path for Isolation Forest model.</param>
    /// <param name="lightGbmPath">Path for LightGBM model.</param>
    public void SetModelPaths(string isolationForestPath, string lightGbmPath)
    {
        _isolationForestModelPath = isolationForestPath ?? throw new ArgumentNullException(nameof(isolationForestPath));
        _lightGbmModelPath = lightGbmPath ?? throw new ArgumentNullException(nameof(lightGbmPath));
    }

    /// <summary>
    /// Trains both models on the provided data.
    /// Step 1: Extract features from all transactions
    /// Step 2: Train Isolation Forest on feature vectors
    /// Step 3: Generate anomaly scores for training data
    /// Step 4: Train LightGBM with combined features (FeatureVector + AnomalyScore)
    /// </summary>
    /// <param name="data">Training data with labeled transactions.</param>
    /// <returns>Training result with metrics and model paths.</returns>
    public TrainingResult Train(TrainingData data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        if (data.Transactions == null || data.Transactions.Length == 0)
        {
            throw new ArgumentException("Training data must contain transactions.", nameof(data));
        }

        // Validate all transactions have labels
        if (data.Transactions.Any(t => t.IsFraud == null))
        {
            throw new ArgumentException("All transactions must have IsFraud label for training.", nameof(data));
        }

        try
        {
            // Step 1: Extract features from all transactions
            // Note: Normalization is handled by ONNX models (sklearn Pipeline with MinMaxScaler)
            var featureVectors = _featureExtractor.ExtractBatch(data.Transactions);
            
            // Filter out null feature vectors and get corresponding labels
            var validData = new List<(FeatureVector Features, bool Label)>();
            for (int i = 0; i < featureVectors.Length; i++)
            {
                if (featureVectors[i] != null)
                {
                    validData.Add((featureVectors[i]!, data.Transactions[i].IsFraud!.Value));
                }
            }

            if (validData.Count == 0)
            {
                throw new InvalidOperationException("No valid feature vectors extracted from training data.");
            }

            var validFeatures = validData.Select(d => d.Features).ToArray();
            var validLabels = validData.Select(d => d.Label).ToArray();

            // Step 2: Train Isolation Forest on feature vectors
            _anomalyDetector.Train(validFeatures);

            // Step 3: Generate anomaly scores for training data
            var anomalyResults = _anomalyDetector.PredictBatch(validFeatures);

            // Step 4: Create classification inputs (FeatureVector + AnomalyScore)
            var classificationInputs = new ClassificationInput[validFeatures.Length];
            for (int i = 0; i < validFeatures.Length; i++)
            {
                classificationInputs[i] = new ClassificationInput
                {
                    Features = validFeatures[i],
                    AnomalyScore = anomalyResults[i].AnomalyScore
                };
            }

            // Step 5: Train LightGBM with combined features
            _fraudClassifier.Train(classificationInputs, validLabels);

            // Step 6: Save both models to disk
            _anomalyDetector.SaveModel(_isolationForestModelPath);
            _fraudClassifier.SaveModel(_lightGbmModelPath);

            // Step 7: Calculate metrics on training data
            var metrics = CalculateMetrics(classificationInputs, validLabels);

            return new TrainingResult
            {
                Success = true,
                Metrics = metrics,
                IsolationForestModelPath = _isolationForestModelPath,
                LightGbmModelPath = _lightGbmModelPath
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Training pipeline error: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return new TrainingResult
            {
                Success = false,
                Metrics = new ModelMetrics(),
                IsolationForestModelPath = string.Empty,
                LightGbmModelPath = string.Empty
            };
        }
    }

    /// <summary>
    /// Evaluates the trained models on test data.
    /// </summary>
    /// <param name="testData">Test data with labeled transactions.</param>
    /// <returns>Model performance metrics.</returns>
    public ModelMetrics Evaluate(TrainingData testData)
    {
        if (testData == null)
        {
            throw new ArgumentNullException(nameof(testData));
        }

        if (testData.Transactions == null || testData.Transactions.Length == 0)
        {
            throw new ArgumentException("Test data must contain transactions.", nameof(testData));
        }

        if (testData.Transactions.Any(t => t.IsFraud == null))
        {
            throw new ArgumentException("All transactions must have IsFraud label for evaluation.", nameof(testData));
        }

        // Extract features
        var featureVectors = _featureExtractor.ExtractBatch(testData.Transactions);
        
        // Filter valid data
        var validData = new List<(FeatureVector Features, bool Label)>();
        for (int i = 0; i < featureVectors.Length; i++)
        {
            if (featureVectors[i] != null)
            {
                validData.Add((featureVectors[i]!, testData.Transactions[i].IsFraud!.Value));
            }
        }

        if (validData.Count == 0)
        {
            return new ModelMetrics();
        }

        var validFeatures = validData.Select(d => d.Features).ToArray();
        var validLabels = validData.Select(d => d.Label).ToArray();

        // Get anomaly scores
        var anomalyResults = _anomalyDetector.PredictBatch(validFeatures);

        // Create classification inputs
        var classificationInputs = new ClassificationInput[validFeatures.Length];
        for (int i = 0; i < validFeatures.Length; i++)
        {
            classificationInputs[i] = new ClassificationInput
            {
                Features = validFeatures[i],
                AnomalyScore = anomalyResults[i].AnomalyScore
            };
        }

        return CalculateMetrics(classificationInputs, validLabels);
    }


    /// <summary>
    /// Calculates model performance metrics.
    /// </summary>
    private ModelMetrics CalculateMetrics(ClassificationInput[] inputs, bool[] actualLabels)
    {
        var predictions = _fraudClassifier.PredictBatch(inputs);

        int truePositives = 0;
        int trueNegatives = 0;
        int falsePositives = 0;
        int falseNegatives = 0;

        var confidenceScores = new List<(float Score, bool ActualLabel)>();

        for (int i = 0; i < predictions.Length; i++)
        {
            bool predicted = predictions[i].IsFraudTransaction;
            bool actual = actualLabels[i];

            confidenceScores.Add((predictions[i].ConfidenceScore, actual));

            if (predicted && actual)
                truePositives++;
            else if (!predicted && !actual)
                trueNegatives++;
            else if (predicted && !actual)
                falsePositives++;
            else
                falseNegatives++;
        }

        int total = predictions.Length;
        double accuracy = total > 0 ? (double)(truePositives + trueNegatives) / total : 0;
        double precision = (truePositives + falsePositives) > 0 
            ? (double)truePositives / (truePositives + falsePositives) : 0;
        double recall = (truePositives + falseNegatives) > 0 
            ? (double)truePositives / (truePositives + falseNegatives) : 0;
        double f1Score = (precision + recall) > 0 
            ? 2 * (precision * recall) / (precision + recall) : 0;
        double aucRoc = CalculateAucRoc(confidenceScores);

        return new ModelMetrics
        {
            Accuracy = accuracy,
            Precision = precision,
            Recall = recall,
            F1Score = f1Score,
            AucRoc = aucRoc
        };
    }

    /// <summary>
    /// Calculates AUC-ROC using the trapezoidal rule.
    /// </summary>
    private static double CalculateAucRoc(List<(float Score, bool ActualLabel)> predictions)
    {
        if (predictions.Count == 0)
            return 0;

        // Sort by score descending
        var sorted = predictions.OrderByDescending(p => p.Score).ToList();

        int totalPositives = sorted.Count(p => p.ActualLabel);
        int totalNegatives = sorted.Count - totalPositives;

        if (totalPositives == 0 || totalNegatives == 0)
            return 0.5; // No discrimination possible

        double auc = 0;
        int tp = 0;
        int fp = 0;
        double prevTpr = 0;
        double prevFpr = 0;

        foreach (var (score, actualLabel) in sorted)
        {
            if (actualLabel)
                tp++;
            else
                fp++;

            double tpr = (double)tp / totalPositives;
            double fpr = (double)fp / totalNegatives;

            // Trapezoidal rule
            auc += (fpr - prevFpr) * (tpr + prevTpr) / 2;

            prevTpr = tpr;
            prevFpr = fpr;
        }

        return auc;
    }
}
