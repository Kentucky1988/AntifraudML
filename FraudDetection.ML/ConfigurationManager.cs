using System.Text.Json;
using FraudDetection.Core.Configuration;
using FraudDetection.Core.Interfaces;

namespace FraudDetection.ML;

/// <summary>
/// Manages configuration for ML models with support for JSON files and environment variables.
/// </summary>
public class ConfigurationManager : IConfigurationManager
{
    private IsolationForestConfig _isolationForestConfig;
    private LightGbmConfig _lightGbmConfig;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    /// <summary>
    /// Creates a ConfigurationManager with default configurations.
    /// </summary>
    public ConfigurationManager()
    {
        _isolationForestConfig = new IsolationForestConfig();
        _lightGbmConfig = new LightGbmConfig();
    }

    /// <summary>
    /// Creates a ConfigurationManager with specified configurations.
    /// </summary>
    public ConfigurationManager(IsolationForestConfig isolationForestConfig, LightGbmConfig lightGbmConfig)
    {
        _isolationForestConfig = isolationForestConfig ?? throw new ArgumentNullException(nameof(isolationForestConfig));
        _lightGbmConfig = lightGbmConfig ?? throw new ArgumentNullException(nameof(lightGbmConfig));
    }

    /// <inheritdoc />
    public IsolationForestConfig GetIsolationForestConfig() => _isolationForestConfig;

    /// <inheritdoc />
    public LightGbmConfig GetLightGbmConfig() => _lightGbmConfig;

    /// <inheritdoc />
    public void Validate()
    {
        ValidateIsolationForestConfig(_isolationForestConfig);
        ValidateLightGbmConfig(_lightGbmConfig);
    }

    /// <summary>
    /// Validates Isolation Forest configuration parameters.
    /// </summary>
    public static void ValidateIsolationForestConfig(IsolationForestConfig config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        if (config.NumberOfTrees <= 0)
            throw new ArgumentException("NumberOfTrees must be greater than 0", nameof(config));

        if (config.SampleSize <= 0)
            throw new ArgumentException("SampleSize must be greater than 0", nameof(config));

        if (config.ContaminationRate < 0 || config.ContaminationRate > 1)
            throw new ArgumentException("ContaminationRate must be between 0 and 1", nameof(config));

        if (config.AnomalyThreshold < 0 || config.AnomalyThreshold > 1)
            throw new ArgumentException("AnomalyThreshold must be between 0 and 1", nameof(config));
    }

    /// <summary>
    /// Validates LightGBM configuration parameters.
    /// </summary>
    public static void ValidateLightGbmConfig(LightGbmConfig config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        if (config.NumberOfLeaves <= 0)
            throw new ArgumentException("NumberOfLeaves must be greater than 0", nameof(config));

        if (config.MinimumExampleCountPerLeaf <= 0)
            throw new ArgumentException("MinimumExampleCountPerLeaf must be greater than 0", nameof(config));

        if (config.LearningRate <= 0)
            throw new ArgumentException("LearningRate must be greater than 0", nameof(config));

        if (config.NumberOfIterations <= 0)
            throw new ArgumentException("NumberOfIterations must be greater than 0", nameof(config));
    }

    /// <summary>
    /// Loads configuration from a JSON file.
    /// </summary>
    /// <param name="filePath">Path to the JSON configuration file (e.g., appsettings.json)</param>
    /// <returns>A ConfigurationManager with loaded settings</returns>
    public static ConfigurationManager LoadFromJson(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Configuration file not found: {filePath}", filePath);

        var json = File.ReadAllText(filePath);
        var configRoot = JsonSerializer.Deserialize<ConfigurationRoot>(json, JsonOptions);

        var isolationForestConfig = configRoot?.IsolationForest ?? new IsolationForestConfig();
        var lightGbmConfig = configRoot?.LightGbm ?? new LightGbmConfig();

        var manager = new ConfigurationManager(isolationForestConfig, lightGbmConfig);
        manager.ApplyEnvironmentOverrides();
        
        return manager;
    }

    /// <summary>
    /// Saves the current configuration to a JSON file.
    /// </summary>
    /// <param name="filePath">Path to save the JSON configuration file</param>
    public void SaveToJson(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        var configRoot = new ConfigurationRoot
        {
            IsolationForest = _isolationForestConfig,
            LightGbm = _lightGbmConfig
        };

        var json = JsonSerializer.Serialize(configRoot, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Applies environment variable overrides to the configuration.
    /// Environment variables follow the pattern: FRAUD_DETECTION_{SECTION}_{PROPERTY}
    /// </summary>
    public void ApplyEnvironmentOverrides()
    {
        // Isolation Forest overrides
        ApplyEnvOverride("FRAUD_DETECTION_IF_NUMBER_OF_TREES", 
            value => _isolationForestConfig.NumberOfTrees = int.Parse(value));
        ApplyEnvOverride("FRAUD_DETECTION_IF_SAMPLE_SIZE", 
            value => _isolationForestConfig.SampleSize = int.Parse(value));
        ApplyEnvOverride("FRAUD_DETECTION_IF_CONTAMINATION_RATE", 
            value => _isolationForestConfig.ContaminationRate = float.Parse(value));
        ApplyEnvOverride("FRAUD_DETECTION_IF_ANOMALY_THRESHOLD", 
            value => _isolationForestConfig.AnomalyThreshold = float.Parse(value));

        // LightGBM overrides
        ApplyEnvOverride("FRAUD_DETECTION_LGBM_NUMBER_OF_LEAVES", 
            value => _lightGbmConfig.NumberOfLeaves = int.Parse(value));
        ApplyEnvOverride("FRAUD_DETECTION_LGBM_MIN_EXAMPLES_PER_LEAF", 
            value => _lightGbmConfig.MinimumExampleCountPerLeaf = int.Parse(value));
        ApplyEnvOverride("FRAUD_DETECTION_LGBM_LEARNING_RATE", 
            value => _lightGbmConfig.LearningRate = double.Parse(value));
        ApplyEnvOverride("FRAUD_DETECTION_LGBM_NUMBER_OF_ITERATIONS", 
            value => _lightGbmConfig.NumberOfIterations = int.Parse(value));
    }

    private static void ApplyEnvOverride(string envVarName, Action<string> setter)
    {
        var value = Environment.GetEnvironmentVariable(envVarName);
        if (!string.IsNullOrWhiteSpace(value))
        {
            setter(value);
        }
    }

    /// <summary>
    /// Updates the Isolation Forest configuration.
    /// </summary>
    public void SetIsolationForestConfig(IsolationForestConfig config)
    {
        _isolationForestConfig = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Updates the LightGBM configuration.
    /// </summary>
    public void SetLightGbmConfig(LightGbmConfig config)
    {
        _lightGbmConfig = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Root configuration object for JSON serialization.
    /// </summary>
    private class ConfigurationRoot
    {
        public IsolationForestConfig? IsolationForest { get; set; }
        public LightGbmConfig? LightGbm { get; set; }
    }
}
