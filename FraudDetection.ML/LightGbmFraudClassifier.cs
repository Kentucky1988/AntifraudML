using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers.LightGbm;
using FraudDetection.Core.Configuration;
using FraudDetection.Core.Interfaces;
using FraudDetection.Core.Models;

namespace FraudDetection.ML;

/// <summary>
/// LightGBM based fraud classifier implementation using ML.NET.
/// </summary>
public class LightGbmFraudClassifier : IFraudClassifier
{
    private readonly LightGbmConfig _config;
    private readonly MLContext _mlContext;
    private ITransformer? _model;
    private PredictionEngine<ClassificationFeatures, FraudPredictionOutput>? _predictionEngine;
    private bool _isTrained;
    private int _featureCount;

    /// <summary>
    /// Creates a new LightGbmFraudClassifier with the specified configuration.
    /// </summary>
    /// <param name="config">Configuration parameters for LightGBM.</param>
    public LightGbmFraudClassifier(LightGbmConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _mlContext = new MLContext(seed: 42);
        _isTrained = false;
    }

    /// <summary>
    /// Creates a new LightGbmFraudClassifier with default configuration.
    /// </summary>
    public LightGbmFraudClassifier() : this(new LightGbmConfig()) { }

    /// <summary>
    /// Gets whether the model has been trained.
    /// </summary>
    public bool IsTrained => _isTrained;

    /// <summary>
    /// Trains the classifier on labeled data.
    /// </summary>
    /// <param name="trainingData">Array of classification inputs (features + anomaly score).</param>
    /// <param name="labels">Array of fraud labels (true = fraud, false = legitimate).</param>
    public void Train(ClassificationInput[] trainingData, bool[] labels)
    {
        if (trainingData == null || trainingData.Length == 0)
        {
            throw new ArgumentException("Training data cannot be null or empty.", nameof(trainingData));
        }

        if (labels == null || labels.Length == 0)
        {
            throw new ArgumentException("Labels cannot be null or empty.", nameof(labels));
        }

        if (trainingData.Length != labels.Length)
        {
            throw new ArgumentException("Training data and labels must have the same length.");
        }

        // Validate and convert to ML.NET format
        var mlData = new List<ClassificationFeatures>();
        for (int i = 0; i < trainingData.Length; i++)
        {
            var input = trainingData[i];
            if (input?.Features?.Features == null || input.Features.Features.Length == 0)
            {
                continue; // Skip invalid inputs
            }

            mlData.Add(CreateClassificationFeatures(input, labels[i]));
        }

        if (mlData.Count == 0)
        {
            throw new ArgumentException("No valid training data after filtering.", nameof(trainingData));
        }

        // Store feature count for validation during prediction
        _featureCount = mlData[0].Features.Length;

        // Create schema definition with known vector size
        var schemaDefinition = SchemaDefinition.Create(typeof(ClassificationFeatures));
        schemaDefinition["Features"].ColumnType = new VectorDataViewType(NumberDataViewType.Single, _featureCount);

        // Load data into ML.NET with explicit schema
        var dataView = _mlContext.Data.LoadFromEnumerable(mlData, schemaDefinition);

        // Build the pipeline
        var pipeline = _mlContext.Transforms.CopyColumns(outputColumnName: "Label", inputColumnName: nameof(ClassificationFeatures.Label))
            .Append(_mlContext.Transforms.NormalizeMinMax(outputColumnName: "NormalizedFeatures", inputColumnName: nameof(ClassificationFeatures.Features)))
            .Append(_mlContext.BinaryClassification.Trainers.LightGbm(
                labelColumnName: "Label",
                featureColumnName: "NormalizedFeatures",
                numberOfLeaves: _config.NumberOfLeaves,
                minimumExampleCountPerLeaf: _config.MinimumExampleCountPerLeaf,
                learningRate: _config.LearningRate,
                numberOfIterations: _config.NumberOfIterations));

        // Train the model
        _model = pipeline.Fit(dataView);

        // Create prediction engine with explicit input schema
        var inputSchemaDefinition = SchemaDefinition.Create(typeof(ClassificationFeatures));
        inputSchemaDefinition["Features"].ColumnType = new VectorDataViewType(NumberDataViewType.Single, _featureCount);
        _predictionEngine = _mlContext.Model.CreatePredictionEngine<ClassificationFeatures, FraudPredictionOutput>(
            _model, 
            inputSchemaDefinition: inputSchemaDefinition);

        _isTrained = true;
    }

    /// <summary>
    /// Predicts fraud for a single input.
    /// </summary>
    /// <param name="input">Classification input containing features and anomaly score.</param>
    /// <returns>Fraud prediction with confidence score.</returns>
    public FraudPrediction Predict(ClassificationInput input)
    {
        if (!_isTrained || _predictionEngine == null)
        {
            throw new InvalidOperationException("Model has not been trained. Call Train() or LoadModel() first.");
        }

        if (input?.Features?.Features == null || input.Features.Features.Length == 0)
        {
            throw new ArgumentException("Input features cannot be null or empty.", nameof(input));
        }

        var classificationFeatures = CreateClassificationFeatures(input, false);
        var prediction = _predictionEngine.Predict(classificationFeatures);

        return new FraudPrediction
        {
            TransactionId = input.Features.TransactionId,
            IsFraudTransaction = prediction.IsFraud,
            ConfidenceScore = Math.Clamp(prediction.Probability, 0f, 1f),
            AnomalyScore = input.AnomalyScore
        };
    }

    /// <summary>
    /// Predicts fraud for multiple inputs.
    /// </summary>
    /// <param name="inputs">Array of classification inputs.</param>
    /// <returns>Array of fraud predictions.</returns>
    public FraudPrediction[] PredictBatch(ClassificationInput[] inputs)
    {
        if (inputs == null)
        {
            return Array.Empty<FraudPrediction>();
        }

        var results = new FraudPrediction[inputs.Length];
        for (int i = 0; i < inputs.Length; i++)
        {
            results[i] = Predict(inputs[i]);
        }
        return results;
    }

    /// <summary>
    /// Saves the trained model to disk using ML.NET API.
    /// </summary>
    /// <param name="path">File path to save the model.</param>
    public void SaveModel(string path)
    {
        if (!_isTrained || _model == null)
        {
            throw new InvalidOperationException("Model has not been trained. Call Train() first.");
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));
        }

        // Ensure directory exists
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Create schema with known vector size for saving
        var schemaDefinition = SchemaDefinition.Create(typeof(ClassificationFeatures));
        schemaDefinition["Features"].ColumnType = new VectorDataViewType(NumberDataViewType.Single, _featureCount);
        var emptyData = _mlContext.Data.LoadFromEnumerable(Array.Empty<ClassificationFeatures>(), schemaDefinition);

        _mlContext.Model.Save(_model, emptyData.Schema, path);
        
        // Save feature count to a separate file
        var metadataPath = path + ".meta";
        File.WriteAllText(metadataPath, _featureCount.ToString());
    }

    /// <summary>
    /// Loads a trained model from disk.
    /// </summary>
    /// <param name="path">File path to load the model from.</param>
    public void LoadModel(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Model file not found.", path);
        }

        // Load feature count from metadata file
        var metadataPath = path + ".meta";
        if (File.Exists(metadataPath))
        {
            _featureCount = int.Parse(File.ReadAllText(metadataPath));
        }
        else
        {
            // Default to DepositCounter count + 1 (for anomaly score)
            _featureCount = FraudDetection.Core.Models.DepositCounter.TotalCounterCount + 1;
        }

        _model = _mlContext.Model.Load(path, out _);

        if (_model == null)
        {
            throw new InvalidDataException("Failed to load model from file.");
        }

        // Create prediction engine with explicit input schema
        var inputSchemaDefinition = SchemaDefinition.Create(typeof(ClassificationFeatures));
        inputSchemaDefinition["Features"].ColumnType = new VectorDataViewType(NumberDataViewType.Single, _featureCount);
        _predictionEngine = _mlContext.Model.CreatePredictionEngine<ClassificationFeatures, FraudPredictionOutput>(
            _model,
            inputSchemaDefinition: inputSchemaDefinition);

        _isTrained = true;
    }

    /// <summary>
    /// Creates ClassificationFeatures from ClassificationInput.
    /// Combines feature vector with anomaly score.
    /// </summary>
    private static ClassificationFeatures CreateClassificationFeatures(ClassificationInput input, bool label)
    {
        // Combine original features with anomaly score
        var originalFeatures = input.Features.Features;
        var combinedFeatures = new float[originalFeatures.Length + 1];
        Array.Copy(originalFeatures, combinedFeatures, originalFeatures.Length);
        combinedFeatures[originalFeatures.Length] = input.AnomalyScore;

        return new ClassificationFeatures
        {
            Features = combinedFeatures,
            Label = label
        };
    }
}

/// <summary>
/// ML.NET input class for LightGBM classification.
/// Contains combined features (original features + anomaly score).
/// </summary>
public class ClassificationFeatures
{
    /// <summary>
    /// Combined feature vector (original features + anomaly score).
    /// </summary>
    [VectorType]
    public float[] Features { get; set; } = Array.Empty<float>();

    /// <summary>
    /// Fraud label (true = fraud, false = legitimate).
    /// </summary>
    public bool Label { get; set; }
}

/// <summary>
/// ML.NET output class for fraud prediction.
/// </summary>
public class FraudPredictionOutput
{
    /// <summary>
    /// Predicted fraud label.
    /// </summary>
    [ColumnName("PredictedLabel")]
    public bool IsFraud { get; set; }

    /// <summary>
    /// Probability of fraud (confidence score).
    /// </summary>
    [ColumnName("Probability")]
    public float Probability { get; set; }

    /// <summary>
    /// Raw score from the model.
    /// </summary>
    [ColumnName("Score")]
    public float Score { get; set; }
}
