using FraudDetection.Core.Interfaces;
using FraudDetection.Core.Models;

namespace FraudDetection.ML;

/// <summary>
/// Training pipeline that orchestrates the training of both Isolation Forest
/// and LightGBM models in sequence. Optimized for large datasets (200k+ records).
/// </summary>
public class TrainingPipeline : ITrainingPipeline
{
    private readonly IFeatureExtractor _featureExtractor;
    private readonly IAnomalyDetector _anomalyDetector;
    private readonly IFraudClassifier _fraudClassifier;
    
    private string _isolationForestModelPath = "isolation_forest.onnx";
    private string _lightGbmModelPath = "lightgbm.onnx";

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
    /// Optimized for 200k+ records with minimal allocations.
    /// 
    /// Pipeline steps:
    /// 1. Extract features from all transactions (single pass)
    /// 2. Train Isolation Forest on feature vectors
    /// 3. Generate anomaly scores for training data
    /// 4. Train LightGBM with combined features (FeatureVector + AnomalyScore)
    /// 5. Save models to disk
    /// 6. Calculate training metrics
    /// </summary>
    /// <param name="data">Training data with labeled transactions.</param>
    /// <returns>Training result with metrics and model paths.</returns>
    public TrainingResult Train(TrainingData data)
    {
        if (data?.Transactions == null || data.Transactions.Length == 0)
            throw new ArgumentException("Training data must contain transactions.", nameof(data));

        try
        {
            var transactions = data.Transactions;
            int count = transactions.Length;

            // Pre-allocate arrays to avoid List resizing for large datasets
            var featureVectors = new FeatureVector[count];
            var labels = new bool[count];
            int validCount = 0;

            // Single pass: extract features and labels
            for (int i = 0; i < count; i++)
            {
                var t = transactions[i];
                if (t.IsFraud == null)
                    throw new ArgumentException($"Transaction at index {i} missing IsFraud label.", nameof(data));

                var fv = _featureExtractor.Extract(t);
                if (fv != null)
                {
                    featureVectors[validCount] = fv;
                    labels[validCount] = t.IsFraud.Value;
                    validCount++;
                }
            }

            if (validCount == 0)
                throw new InvalidOperationException("No valid feature vectors extracted from training data.");

            // Resize only if there were invalid entries (rare case)
            if (validCount < count)
            {
                Array.Resize(ref featureVectors, validCount);
                Array.Resize(ref labels, validCount);
            }

            // Step 1: Train Isolation Forest on feature vectors
            _anomalyDetector.Train(featureVectors);

            // Step 2: Generate anomaly scores for training data
            var anomalyResults = _anomalyDetector.PredictBatch(featureVectors);

            // Step 3: Combine features + anomaly score into single array for LightGBM
            int featureLength = featureVectors[0].Features.Length;
            var combinedFeatures = new float[validCount][];
            
            for (int i = 0; i < validCount; i++)
            {
                var combined = new float[featureLength + 1];
                Array.Copy(featureVectors[i].Features, combined, featureLength);
                combined[featureLength] = anomalyResults[i].AnomalyScore;
                combinedFeatures[i] = combined;
            }

            // Step 4: Train LightGBM with combined features
            _fraudClassifier.Train(combinedFeatures, labels);

            // Step 5: Save both models to disk
            _anomalyDetector.SaveModel(_isolationForestModelPath);
            _fraudClassifier.SaveModel(_lightGbmModelPath);

            // Step 6: Build ClassificationInputs for metrics calculation (needed for Predict interface)
            var classificationInputs = new ClassificationInput[validCount];
            for (int i = 0; i < validCount; i++)
            {
                classificationInputs[i] = new ClassificationInput
                {
                    Features = featureVectors[i],
                    AnomalyScore = anomalyResults[i].AnomalyScore
                };
            }

            // Step 7: Calculate metrics on training data
            var metrics = CalculateMetrics(classificationInputs, labels);

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
        if (testData?.Transactions == null || testData.Transactions.Length == 0)
            throw new ArgumentException("Test data must contain transactions.", nameof(testData));

        var transactions = testData.Transactions;
        int count = transactions.Length;

        // Pre-allocate arrays
        var featureVectors = new FeatureVector[count];
        var labels = new bool[count];
        int validCount = 0;

        // Single pass extraction
        for (int i = 0; i < count; i++)
        {
            var t = transactions[i];
            if (t.IsFraud == null) continue;

            var fv = _featureExtractor.Extract(t);
            if (fv != null)
            {
                featureVectors[validCount] = fv;
                labels[validCount] = t.IsFraud.Value;
                validCount++;
            }
        }

        if (validCount == 0)
            return new ModelMetrics();

        // Resize only if needed
        if (validCount < count)
        {
            Array.Resize(ref featureVectors, validCount);
            Array.Resize(ref labels, validCount);
        }

        // Get anomaly scores
        var anomalyResults = _anomalyDetector.PredictBatch(featureVectors);

        // Create classification inputs for Predict interface
        var classificationInputs = new ClassificationInput[validCount];
        for (int i = 0; i < validCount; i++)
        {
            classificationInputs[i] = new ClassificationInput
            {
                Features = featureVectors[i],
                AnomalyScore = anomalyResults[i].AnomalyScore
            };
        }

        return CalculateMetrics(classificationInputs, labels);
    }

    /// <summary>
    /// Calculates model performance metrics (Accuracy, Precision, Recall, F1, AUC-ROC).
    /// Uses pre-allocated arrays to minimize GC pressure for large datasets.
    /// </summary>
    private ModelMetrics CalculateMetrics(ClassificationInput[] inputs, bool[] actualLabels)
    {
        var predictions = _fraudClassifier.PredictBatch(inputs);
        int count = predictions.Length;

        int tp = 0, tn = 0, fp = 0, fn = 0;

        // Pre-allocate array for AUC calculation (avoid List dynamic resizing)
        var scores = new (float Score, bool Label)[count];

        for (int i = 0; i < count; i++)
        {
            bool predicted = predictions[i].IsFraudTransaction;
            bool actual = actualLabels[i];
            
            scores[i] = (predictions[i].ConfidenceScore, actual);

            if (predicted && actual) tp++;
            else if (!predicted && !actual) tn++;
            else if (predicted) fp++;
            else fn++;
        }

        double accuracy = count > 0 ? (double)(tp + tn) / count : 0;
        double precision = (tp + fp) > 0 ? (double)tp / (tp + fp) : 0;
        double recall = (tp + fn) > 0 ? (double)tp / (tp + fn) : 0;
        double f1 = (precision + recall) > 0 ? 2 * precision * recall / (precision + recall) : 0;

        return new ModelMetrics
        {
            Accuracy = accuracy,
            Precision = precision,
            Recall = recall,
            F1Score = f1,
            AucRoc = CalculateAucRoc(scores)
        };
    }

    /// <summary>
    /// Calculates AUC-ROC using the trapezoidal rule.
    /// Uses Span for in-place sorting to avoid additional allocations.
    /// </summary>
    private static double CalculateAucRoc(Span<(float Score, bool Label)> predictions)
    {
        if (predictions.Length == 0) return 0;

        // Sort in-place by score descending (no additional allocation)
        predictions.Sort((a, b) => b.Score.CompareTo(a.Score));

        int totalPos = 0, totalNeg = 0;
        foreach (var p in predictions)
        {
            if (p.Label) totalPos++;
            else totalNeg++;
        }

        if (totalPos == 0 || totalNeg == 0) return 0.5;

        double auc = 0;
        int tp = 0, fp = 0;
        double prevTpr = 0, prevFpr = 0;

        foreach (var (_, label) in predictions)
        {
            if (label) tp++;
            else fp++;

            double tpr = (double)tp / totalPos;
            double fpr = (double)fp / totalNeg;

            // Trapezoidal rule
            auc += (fpr - prevFpr) * (tpr + prevTpr) / 2;
            prevTpr = tpr;
            prevFpr = fpr;
        }

        return auc;
    }
}
