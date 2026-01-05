using System.Globalization;
using System.Text;
using CliWrap;
using CliWrap.Buffered;
using FraudDetection.Core.Configuration;
using FraudDetection.Core.Interfaces;
using FraudDetection.Core.Models;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace FraudDetection.ML;

/// <summary>
/// Isolation Forest anomaly detector that uses Python sklearn for training
/// and ONNX Runtime for inference.
/// </summary>
public class OnnxIsolationForestDetector : IAnomalyDetector, IDisposable
{
    private readonly IsolationForestConfig _config;
    private readonly string _pythonPath;
    private readonly string _scriptPath;
    private InferenceSession? _session;
    private byte[]? _modelBytes;
    private bool _disposed;

    /// <summary>
    /// Creates a new OnnxIsolationForestDetector with the specified configuration.
    /// </summary>
    /// <param name="config">Configuration parameters for the forest.</param>
    /// <param name="pythonPath">Path to Python executable. If null, uses PYTHON_PATH env var or "python".</param>
    /// <param name="scriptPath">Path to training script (default: "Scripts/train_isolation_forest.py").</param>
    public OnnxIsolationForestDetector(
        IsolationForestConfig? config = null,
        string? pythonPath = null,
        string? scriptPath = null)
    {
        _config = config ?? new IsolationForestConfig();
        _pythonPath = pythonPath ?? Environment.GetEnvironmentVariable("PYTHON_PATH") ?? "python";
        _scriptPath = scriptPath ?? GetDefaultScriptPath();
    }

    /// <summary>
    /// Gets whether the model has been loaded and is ready for inference.
    /// </summary>
    public bool IsTrained => _session != null;

    /// <summary>
    /// Trains the Isolation Forest using Python sklearn and saves as ONNX.
    /// </summary>
    /// <param name="trainingData">Array of feature vectors to train on.</param>
    public void Train(FeatureVector[] trainingData)
    {
        TrainAsync(trainingData).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asynchronously trains the Isolation Forest using Python sklearn.
    /// </summary>
    public async Task TrainAsync(FeatureVector[] trainingData)
    {
        if (trainingData == null || trainingData.Length == 0)
        {
            throw new ArgumentException("Training data cannot be null or empty.", nameof(trainingData));
        }

        var validData = trainingData
            .Where(fv => fv?.Features != null && fv.Features.Length > 0)
            .ToArray();

        if (validData.Length == 0)
        {
            throw new ArgumentException("No valid feature vectors in training data.", nameof(trainingData));
        }

        // Create temp files for CSV and ONNX model
        string tempCsv = Path.Combine(Path.GetTempPath(), $"isolation_forest_train_{Guid.NewGuid()}.csv");
        string tempOnnx = Path.Combine(Path.GetTempPath(), $"isolation_forest_{Guid.NewGuid()}.onnx");

        try
        {
            // Export training data to CSV
            await ExportToCsvAsync(validData, tempCsv);

            // Run Python training script
            bool success = await RunTrainingAsync(tempCsv, tempOnnx);
            if (!success)
            {
                throw new InvalidOperationException("Python training script failed. Check logs for details.");
            }

            // Load the trained ONNX model
            Console.WriteLine($"Loading ONNX model from: {tempOnnx}");
            if (!File.Exists(tempOnnx))
            {
                throw new FileNotFoundException($"ONNX model file not found at: {tempOnnx}");
            }
            Console.WriteLine($"ONNX file size: {new FileInfo(tempOnnx).Length} bytes");
            
            LoadModel(tempOnnx);
            Console.WriteLine("ONNX model loaded successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Training error: {ex.GetType().Name}: {ex.Message}");
            throw;
        }
        finally
        {
            // Cleanup temp files
            if (File.Exists(tempCsv)) File.Delete(tempCsv);
            if (File.Exists(tempOnnx)) File.Delete(tempOnnx);
        }
    }

    /// <summary>
    /// Exports feature vectors to CSV format for Python training.
    /// </summary>
    private static async Task ExportToCsvAsync(FeatureVector[] data, string csvPath)
    {
        var sb = new StringBuilder();
        
        // Header row with feature indices
        int featureCount = data[0].Features.Length;
        var headers = Enumerable.Range(0, featureCount).Select(i => $"f{i}");
        sb.AppendLine(string.Join(",", headers));

        // Data rows
        foreach (var fv in data)
        {
            var values = fv.Features.Select(f => f.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine(string.Join(",", values));
        }

        await File.WriteAllTextAsync(csvPath, sb.ToString());
    }

    /// <summary>
    /// Runs the Python training script using CliWrap.
    /// </summary>
    private async Task<bool> RunTrainingAsync(string inputCsv, string outputOnnx)
    {
        Console.WriteLine("Starting Python Isolation Forest training...");

        try
        {
            var result = await Cli.Wrap(_pythonPath)
                .WithArguments(args => args
                    .Add(_scriptPath)
                    .Add("--input").Add(inputCsv)
                    .Add("--output").Add(outputOnnx)
                    .Add("--trees").Add(_config.NumberOfTrees.ToString()))
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync();

            if (result.ExitCode == 0)
            {
                Console.WriteLine("Python training completed successfully.");
                Console.WriteLine(result.StandardOutput);
                return true;
            }
            else
            {
                Console.WriteLine("Python training failed!");
                Console.WriteLine(result.StandardError);
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to execute Python script: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Predicts anomaly score for a single feature vector using ONNX Runtime.
    /// </summary>
    public AnomalyResult Predict(FeatureVector features)
    {
        if (_session == null)
        {
            throw new InvalidOperationException("Model has not been loaded. Call Train() or LoadModel() first.");
        }

        if (features?.Features == null || features.Features.Length == 0)
        {
            throw new ArgumentException("Feature vector cannot be null or empty.", nameof(features));
        }

        try
        {
            // Create input tensor [1, feature_count]
            var inputTensor = new DenseTensor<float>(features.Features, new[] { 1, features.Features.Length });
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("float_input", inputTensor)
            };

            // Run inference
            using var results = _session.Run(inputs);
            
            // Get anomaly score from output
            // sklearn IsolationForest ONNX outputs: label (1=normal, -1=anomaly) and scores
            float anomalyScore = GetAnomalyScore(results);

            return new AnomalyResult
            {
                TransactionId = features.TransactionId,
                AnomalyScore = anomalyScore,
                IsAnomaly = anomalyScore >= _config.AnomalyThreshold
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Predict error: {ex.GetType().Name}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Extracts anomaly score from ONNX model output.
    /// </summary>
    private static float GetAnomalyScore(IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results)
    {
        // ONNX IsolationForest typically outputs:
        // - "label": predicted labels (-1 for anomaly, 1 for normal)
        // - "scores": decision function scores (negative = anomaly)
        
        var scoresOutput = results.FirstOrDefault(r => r.Name == "scores");
        if (scoresOutput != null)
        {
            var scores = scoresOutput.AsEnumerable<float>().ToArray();
            if (scores.Length > 0)
            {
                // Convert sklearn score to 0-1 range
                // sklearn: negative = anomaly, positive = normal
                // We want: higher = more anomalous
                float rawScore = scores[0];
                // Normalize using sigmoid-like transformation
                float normalizedScore = 1f / (1f + (float)Math.Exp(rawScore * 2));
                return Math.Clamp(normalizedScore, 0f, 1f);
            }
        }

        // Fallback: check label output
        var labelOutput = results.FirstOrDefault(r => r.Name == "label");
        if (labelOutput != null)
        {
            var labels = labelOutput.AsEnumerable<long>().ToArray();
            if (labels.Length > 0)
            {
                // -1 = anomaly (return 1.0), 1 = normal (return 0.0)
                return labels[0] == -1 ? 1.0f : 0.0f;
            }
        }

        return 0f;
    }

    /// <summary>
    /// Predicts anomaly scores for multiple feature vectors.
    /// </summary>
    public AnomalyResult[] PredictBatch(FeatureVector[] features)
    {
        if (features == null)
        {
            return Array.Empty<AnomalyResult>();
        }

        // For batch prediction, we could optimize by creating a single tensor
        // but for simplicity, we process one at a time
        var results = new AnomalyResult[features.Length];
        for (int i = 0; i < features.Length; i++)
        {
            results[i] = Predict(features[i]);
        }
        return results;
    }

    /// <summary>
    /// Saves the ONNX model to the specified path.
    /// </summary>
    public void SaveModel(string path)
    {
        if (_session == null || _modelBytes == null)
        {
            throw new InvalidOperationException("Model has not been loaded. Call Train() or LoadModel() first.");
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));
        }

        // Ensure directory exists
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllBytes(path, _modelBytes);
    }

    /// <summary>
    /// Loads an ONNX model from disk.
    /// </summary>
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

        // Dispose existing session if any
        _session?.Dispose();

        // Read and store model bytes for later saving
        _modelBytes = File.ReadAllBytes(path);

        // Load new session
        var sessionOptions = new SessionOptions();
        _session = new InferenceSession(_modelBytes, sessionOptions);
    }

    /// <summary>
    /// Gets the default path to the training script.
    /// </summary>
    private static string GetDefaultScriptPath()
    {
        // Try to find the script relative to the executing assembly
        var assemblyDir = Path.GetDirectoryName(typeof(OnnxIsolationForestDetector).Assembly.Location);
        if (!string.IsNullOrEmpty(assemblyDir))
        {
            var scriptPath = Path.Combine(assemblyDir, "Scripts", "train_isolation_forest.py");
            if (File.Exists(scriptPath))
            {
                return scriptPath;
            }
        }

        // Fallback to relative path
        return "Scripts/train_isolation_forest.py";
    }

    /// <summary>
    /// Disposes the ONNX inference session.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _session?.Dispose();
                _session = null;
            }
            _disposed = true;
        }
    }
}
