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
/// LightGBM fraud classifier that uses Python for training and ONNX Runtime for inference.
/// </summary>
public class OnnxLightGbmClassifier : IFraudClassifier, IDisposable
{
    private readonly LightGbmConfig _config;
    private readonly string _pythonPath;
    private readonly string _scriptPath;
    private InferenceSession? _session;
    private byte[]? _modelBytes;
    private int _featureCount;
    private bool _disposed;

    /// <summary>
    /// Creates a new OnnxLightGbmClassifier with the specified configuration.
    /// </summary>
    /// <param name="config">LightGBM configuration parameters.</param>
    /// <param name="pythonPath">Path to Python executable. If null, uses PYTHON_PATH env var or "python".</param>
    /// <param name="scriptPath">Path to training script.</param>
    public OnnxLightGbmClassifier(
        LightGbmConfig? config = null,
        string? pythonPath = null,
        string? scriptPath = null)
    {
        _config = config ?? new LightGbmConfig();
        _pythonPath = pythonPath ?? Environment.GetEnvironmentVariable("PYTHON_PATH") ?? "python";
        _scriptPath = scriptPath ?? GetDefaultScriptPath();
    }

    /// <summary>
    /// Gets whether the model has been loaded and is ready for inference.
    /// </summary>
    public bool IsTrained => _session != null;

    /// <summary>
    /// Trains the classifier on labeled data.
    /// </summary>
    /// <param name="features">Pre-combined feature arrays (features + anomaly score as last element).</param>
    /// <param name="labels">Fraud labels (true = fraud).</param>
    public void Train(float[][] features, bool[] labels)
    {
        TrainAsync(features, labels).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asynchronously trains the classifier using Python LightGBM.
    /// </summary>
    public async Task TrainAsync(float[][] features, bool[] labels)
    {
        if (features == null || features.Length == 0)
            throw new ArgumentException("Features cannot be null or empty.", nameof(features));

        if (labels == null || labels.Length != features.Length)
            throw new ArgumentException("Labels must match features length.", nameof(labels));

        _featureCount = features[0].Length;

        string tempCsv = Path.Combine(Path.GetTempPath(), $"lightgbm_train_{Guid.NewGuid()}.csv");
        string tempOnnx = Path.Combine(Path.GetTempPath(), $"lightgbm_{Guid.NewGuid()}.onnx");

        try
        {
            await ExportToCsvAsync(features, labels, tempCsv);
            
            bool success = await RunTrainingAsync(tempCsv, tempOnnx);
            if (!success)
                throw new InvalidOperationException("Python LightGBM training failed.");

            LoadModel(tempOnnx);
        }
        finally
        {
            if (File.Exists(tempCsv)) File.Delete(tempCsv);
            if (File.Exists(tempOnnx)) File.Delete(tempOnnx);
        }
    }

    /// <summary>
    /// Exports training data to CSV format for Python training.
    /// Optimized for large datasets - writes directly without intermediate collections.
    /// </summary>
    private static async Task ExportToCsvAsync(float[][] features, bool[] labels, string csvPath)
    {
        using var writer = new StreamWriter(csvPath, false, Encoding.UTF8, bufferSize: 65536);
        
        // Header
        int featureCount = features[0].Length;
        var header = new StringBuilder();
        for (int i = 0; i < featureCount; i++)
        {
            if (i > 0) header.Append(',');
            header.Append('f').Append(i);
        }
        header.Append(",label");
        await writer.WriteLineAsync(header.ToString());

        // Data rows - write directly without StringBuilder per row for large datasets
        var rowBuffer = new StringBuilder();
        for (int row = 0; row < features.Length; row++)
        {
            rowBuffer.Clear();
            var rowFeatures = features[row];
            
            for (int i = 0; i < rowFeatures.Length; i++)
            {
                if (i > 0) rowBuffer.Append(',');
                rowBuffer.Append(rowFeatures[i].ToString(CultureInfo.InvariantCulture));
            }
            rowBuffer.Append(',').Append(labels[row] ? '1' : '0');
            
            await writer.WriteLineAsync(rowBuffer.ToString());
        }
    }

    /// <summary>
    /// Runs the Python training script using CliWrap.
    /// </summary>
    private async Task<bool> RunTrainingAsync(string inputCsv, string outputOnnx)
    {
        Console.WriteLine("Starting Python LightGBM training...");

        try
        {
            var result = await Cli.Wrap(_pythonPath)
                .WithArguments(args => args
                    .Add(_scriptPath)
                    .Add("--input").Add(inputCsv)
                    .Add("--output").Add(outputOnnx)
                    .Add("--leaves").Add(_config.NumberOfLeaves.ToString())
                    .Add("--lr").Add(_config.LearningRate.ToString(CultureInfo.InvariantCulture))
                    .Add("--iterations").Add(_config.NumberOfIterations.ToString()))
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync();

            if (result.ExitCode == 0)
            {
                Console.WriteLine("Python LightGBM training completed.");
                Console.WriteLine(result.StandardOutput);
                return true;
            }
            else
            {
                Console.WriteLine("Python LightGBM training failed!");
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
    /// Predicts fraud for a single input.
    /// </summary>
    public FraudPrediction Predict(ClassificationInput input)
    {
        if (_session == null)
            throw new InvalidOperationException("Model not loaded. Call Train() or LoadModel() first.");

        if (input?.Features?.Features == null)
            throw new ArgumentException("Input features cannot be null.", nameof(input));

        // Combine features with anomaly score
        var combined = new float[input.Features.Features.Length + 1];
        Array.Copy(input.Features.Features, combined, input.Features.Features.Length);
        combined[^1] = input.AnomalyScore;

        var inputTensor = new DenseTensor<float>(combined, new[] { 1, combined.Length });
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("float_input", inputTensor)
        };

        using var results = _session.Run(inputs);
        
        var (isFraud, probability) = GetPrediction(results);

        return new FraudPrediction
        {
            TransactionId = input.Features.TransactionId,
            IsFraudTransaction = isFraud,
            ConfidenceScore = probability,
            AnomalyScore = input.AnomalyScore
        };
    }

    /// <summary>
    /// Extracts prediction from ONNX model output.
    /// </summary>
    private static (bool IsFraud, float Probability) GetPrediction(IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results)
    {
        var labelOutput = results.FirstOrDefault(r => r.Name == "label");
        if (labelOutput != null)
        {
            try
            {
                var labels = labelOutput.AsEnumerable<long>()?.ToArray();
                if (labels != null && labels.Length > 0)
                {
                    bool isFraud = labels[0] == 1;
                    return (isFraud, isFraud ? 1f : 0f);
                }
            }
            catch { }
        }

        return (false, 0f);
    }

    /// <summary>
    /// Predicts fraud for multiple inputs.
    /// </summary>
    public FraudPrediction[] PredictBatch(ClassificationInput[] inputs)
    {
        if (inputs == null) return Array.Empty<FraudPrediction>();
        
        var results = new FraudPrediction[inputs.Length];
        for (int i = 0; i < inputs.Length; i++)
        {
            results[i] = Predict(inputs[i]);
        }
        return results;
    }

    /// <summary>
    /// Saves the trained model to disk.
    /// </summary>
    public void SaveModel(string path)
    {
        if (_session == null || _modelBytes == null)
            throw new InvalidOperationException("Model not loaded.");

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllBytes(path, _modelBytes);
        File.WriteAllText(path + ".meta", _featureCount.ToString());
    }

    /// <summary>
    /// Loads a trained model from disk.
    /// </summary>
    public void LoadModel(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Model file not found.", path);

        _session?.Dispose();
        _modelBytes = File.ReadAllBytes(path);
        _session = new InferenceSession(_modelBytes);

        var metaPath = path + ".meta";
        if (File.Exists(metaPath))
            _featureCount = int.Parse(File.ReadAllText(metaPath));
    }

    /// <summary>
    /// Gets the default path to the training script.
    /// </summary>
    private static string GetDefaultScriptPath()
    {
        var assemblyDir = Path.GetDirectoryName(typeof(OnnxLightGbmClassifier).Assembly.Location);
        if (!string.IsNullOrEmpty(assemblyDir))
        {
            var scriptPath = Path.Combine(assemblyDir, "Scripts", "train_lightgbm.py");
            if (File.Exists(scriptPath))
                return scriptPath;
        }
        return "Scripts/train_lightgbm.py";
    }

    /// <summary>
    /// Disposes the ONNX inference session.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _session?.Dispose();
            _session = null;
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
