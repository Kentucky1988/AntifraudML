using System.Text.Json;
using FraudDetection.Core.Configuration;
using FraudDetection.Core.Interfaces;
using FraudDetection.Core.Models;

namespace FraudDetection.ML;

/// <summary>
/// Isolation Forest based anomaly detector implementation.
/// Uses an ensemble of isolation trees to detect anomalies based on path length.
/// </summary>
public class IsolationForestDetector : IAnomalyDetector
{
    private readonly IsolationForestConfig _config;
    private List<IsolationTree>? _trees;
    private int _sampleSize;
    private bool _isTrained;

    /// <summary>
    /// Creates a new IsolationForestDetector with the specified configuration.
    /// </summary>
    /// <param name="config">Configuration parameters for the forest.</param>
    public IsolationForestDetector(IsolationForestConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _isTrained = false;
    }

    /// <summary>
    /// Creates a new IsolationForestDetector with default configuration.
    /// </summary>
    public IsolationForestDetector() : this(new IsolationForestConfig()) { }

    /// <summary>
    /// Gets whether the model has been trained.
    /// </summary>
    public bool IsTrained => _isTrained;

    /// <summary>
    /// Trains the Isolation Forest on the provided feature vectors.
    /// </summary>
    /// <param name="trainingData">Array of feature vectors to train on.</param>
    public void Train(FeatureVector[] trainingData)
    {
        if (trainingData == null || trainingData.Length == 0)
        {
            throw new ArgumentException("Training data cannot be null or empty.", nameof(trainingData));
        }

        // Convert FeatureVectors to float arrays
        var data = trainingData
            .Where(fv => fv?.Features != null && fv.Features.Length > 0)
            .Select(fv => fv.Features)
            .ToArray();

        if (data.Length == 0)
        {
            throw new ArgumentException("No valid feature vectors in training data.", nameof(trainingData));
        }

        // Determine sample size (use config or data size, whichever is smaller)
        _sampleSize = Math.Min(_config.SampleSize, data.Length);

        // Calculate max depth: log2(sample_size)
        int maxDepth = (int)Math.Ceiling(Math.Log2(_sampleSize));

        // Build the forest
        _trees = new List<IsolationTree>(_config.NumberOfTrees);
        var random = new Random();

        for (int i = 0; i < _config.NumberOfTrees; i++)
        {
            // Sample data for this tree
            var sample = SampleData(data, _sampleSize, random);

            // Build tree with unique seed for reproducibility within tree
            var tree = new IsolationTree(random.Next());
            tree.Build(sample, maxDepth);
            _trees.Add(tree);
        }

        _isTrained = true;
    }

    /// <summary>
    /// Randomly samples data with replacement.
    /// </summary>
    private static float[][] SampleData(float[][] data, int sampleSize, Random random)
    {
        var sample = new float[sampleSize][];
        for (int i = 0; i < sampleSize; i++)
        {
            int idx = random.Next(data.Length);
            sample[i] = data[idx];
        }
        return sample;
    }

    /// <summary>
    /// Predicts anomaly score for a single feature vector.
    /// </summary>
    /// <param name="features">Feature vector to evaluate.</param>
    /// <returns>Anomaly result with score and flag.</returns>
    public AnomalyResult Predict(FeatureVector features)
    {
        if (!_isTrained || _trees == null)
        {
            throw new InvalidOperationException("Model has not been trained. Call Train() first.");
        }

        if (features?.Features == null || features.Features.Length == 0)
        {
            throw new ArgumentException("Feature vector cannot be null or empty.", nameof(features));
        }

        // Calculate average path length across all trees
        double avgPathLength = 0;
        foreach (var tree in _trees)
        {
            avgPathLength += tree.PathLength(features.Features);
        }
        avgPathLength /= _trees.Count;

        // Calculate anomaly score using the formula:
        // score = 2^(-avgPathLength / c(sampleSize))
        // where c(n) is the average path length of unsuccessful search in BST
        double c = IsolationTree.AveragePathLength(_sampleSize);
        float anomalyScore = (float)Math.Pow(2, -avgPathLength / c);

        // Clamp to [0, 1] range
        anomalyScore = Math.Clamp(anomalyScore, 0f, 1f);

        return new AnomalyResult
        {
            TransactionId = features.TransactionId,
            AnomalyScore = anomalyScore,
            IsAnomaly = anomalyScore >= _config.AnomalyThreshold
        };
    }

    /// <summary>
    /// Predicts anomaly scores for multiple feature vectors.
    /// </summary>
    /// <param name="features">Array of feature vectors to evaluate.</param>
    /// <returns>Array of anomaly results.</returns>
    public AnomalyResult[] PredictBatch(FeatureVector[] features)
    {
        if (features == null)
        {
            return Array.Empty<AnomalyResult>();
        }

        var results = new AnomalyResult[features.Length];
        for (int i = 0; i < features.Length; i++)
        {
            results[i] = Predict(features[i]);
        }
        return results;
    }

    /// <summary>
    /// Saves the trained model to disk using binary serialization.
    /// </summary>
    /// <param name="path">File path to save the model.</param>
    public void SaveModel(string path)
    {
        if (!_isTrained || _trees == null)
        {
            throw new InvalidOperationException("Model has not been trained. Call Train() first.");
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));
        }

        var modelData = new IsolationForestModelData
        {
            Trees = _trees,
            SampleSize = _sampleSize,
            Config = _config
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            IncludeFields = true
        };

        var json = JsonSerializer.Serialize(modelData, options);
        File.WriteAllText(path, json);
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

        var json = File.ReadAllText(path);
        var options = new JsonSerializerOptions
        {
            IncludeFields = true
        };

        var modelData = JsonSerializer.Deserialize<IsolationForestModelData>(json, options);

        if (modelData?.Trees == null || modelData.Trees.Count == 0)
        {
            throw new InvalidDataException("Invalid or corrupted model file.");
        }

        _trees = modelData.Trees;
        _sampleSize = modelData.SampleSize;
        _isTrained = true;
    }
}

/// <summary>
/// Data structure for serializing the Isolation Forest model.
/// </summary>
[Serializable]
internal class IsolationForestModelData
{
    public List<IsolationTree>? Trees { get; set; }
    public int SampleSize { get; set; }
    public IsolationForestConfig? Config { get; set; }
}
