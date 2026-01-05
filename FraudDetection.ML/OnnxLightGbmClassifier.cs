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

    public OnnxLightGbmClassifier(
        LightGbmConfig? config = null,
        string? pythonPath = null,
        string? scriptPath = null)
    {
        _config = config ?? new LightGbmConfig();
        _pythonPath = pythonPath ?? Environment.GetEnvironmentVariable("PYTHON_PATH") ?? "python";
        _scriptPath = scriptPath ?? GetDefaultScriptPath();
    }

    public bool IsTrained => _session != null;

    public void Train(ClassificationInput[] trainingData, bool[] labels)
    {
        TrainAsync(trainingData, labels).GetAwaiter().GetResult();
    }

    public async Task TrainAsync(ClassificationInput[] trainingData, bool[] labels)
    {
        if (trainingData == null || trainingData.Length == 0)
            throw new ArgumentException("Training data cannot be null or empty.", nameof(trainingData));

        if (labels == null || labels.Length != trainingData.Length)
            throw new ArgumentException("Labels must match training data length.", nameof(labels));

        var validData = new List<(float[] Features, bool Label)>();
        for (int i = 0; i < trainingData.Length; i++)
        {
            var input = trainingData[i];
            if (input?.Features?.Features != null && input.Features.Features.Length > 0)
            {
                // Combine features with anomaly score
                var combined = new float[input.Features.Features.Length + 1];
                Array.Copy(input.Features.Features, combined, input.Features.Features.Length);
                combined[^1] = input.AnomalyScore;
                validData.Add((combined, labels[i]));
            }
        }

        if (validData.Count == 0)
            throw new ArgumentException("No valid training data.", nameof(trainingData));

        _featureCount = validData[0].Features.Length;

        string tempCsv = Path.Combine(Path.GetTempPath(), $"lightgbm_train_{Guid.NewGuid()}.csv");
        string tempOnnx = Path.Combine(Path.GetTempPath(), $"lightgbm_{Guid.NewGuid()}.onnx");

        try
        {
            await ExportToCsvAsync(validData, tempCsv);
            
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

    private static async Task ExportToCsvAsync(List<(float[] Features, bool Label)> data, string csvPath)
    {
        var sb = new StringBuilder();
        
        int featureCount = data[0].Features.Length;
        var headers = Enumerable.Range(0, featureCount).Select(i => $"f{i}").Append("label");
        sb.AppendLine(string.Join(",", headers));

        foreach (var (features, label) in data)
        {
            var values = features.Select(f => f.ToString(CultureInfo.InvariantCulture)).Append(label ? "1" : "0");
            sb.AppendLine(string.Join(",", values));
        }

        await File.WriteAllTextAsync(csvPath, sb.ToString());
    }

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

    private static (bool IsFraud, float Probability) GetPrediction(IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results)
    {
        // LightGBM ONNX outputs: label (TENSOR) and probabilities (SEQUENCE)
        
        // Try label output first (more reliable)
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

    public FraudPrediction[] PredictBatch(ClassificationInput[] inputs)
    {
        if (inputs == null) return Array.Empty<FraudPrediction>();
        return inputs.Select(Predict).ToArray();
    }

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
