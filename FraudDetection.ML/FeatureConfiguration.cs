using System.Text.Json;
using FraudDetection.Core.Models;

namespace FraudDetection.ML;

/// <summary>
/// Manages feature selection for ML models.
/// Loads feature names from configuration file to filter out noisy counters.
/// </summary>
public class FeatureConfiguration
{
    private readonly HashSet<string> _selectedFeatures;
    private readonly string[] _orderedFeatures;
    private readonly Dictionary<string, int> _featureIndexMap;

    /// <summary>
    /// Gets the ordered list of selected feature names.
    /// </summary>
    public string[] SelectedFeatures => _orderedFeatures;

    /// <summary>
    /// Gets the number of selected features (excluding anomaly score).
    /// </summary>
    public int FeatureCount => _orderedFeatures.Length;

    /// <summary>
    /// Creates a new FeatureConfiguration from a JSON file.
    /// </summary>
    /// <param name="configPath">Path to feature_names.json</param>
    public FeatureConfiguration(string? configPath = null)
    {
        configPath ??= GetDefaultConfigPath();
        
        if (File.Exists(configPath))
        {
            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<FeatureConfigJson>(json);
            
            if (config?.features != null && config.features.Length > 0)
            {
                // Validate features exist in DepositCounter
                var allCounters = new HashSet<string>(DepositCounter.AllCounterNames);
                var validFeatures = config.features.Where(f => allCounters.Contains(f)).ToArray();
                
                if (validFeatures.Length != config.features.Length)
                {
                    var invalid = config.features.Except(validFeatures);
                    Console.WriteLine($"Warning: Unknown features in config: {string.Join(", ", invalid)}");
                }
                
                _orderedFeatures = validFeatures;
                _selectedFeatures = new HashSet<string>(validFeatures);
            }
            else
            {
                // Fallback to all features
                _orderedFeatures = DepositCounter.AllCounterNames;
                _selectedFeatures = new HashSet<string>(_orderedFeatures);
            }
        }
        else
        {
            // No config file - use all features
            Console.WriteLine($"Feature config not found at {configPath}, using all {DepositCounter.TotalCounterCount} features.");
            _orderedFeatures = DepositCounter.AllCounterNames;
            _selectedFeatures = new HashSet<string>(_orderedFeatures);
        }

        // Build index map for fast lookup
        _featureIndexMap = new Dictionary<string, int>();
        for (int i = 0; i < _orderedFeatures.Length; i++)
        {
            _featureIndexMap[_orderedFeatures[i]] = i;
        }
        
        Console.WriteLine($"Feature configuration loaded: {_orderedFeatures.Length} features selected.");
    }

    /// <summary>
    /// Checks if a feature is selected for use.
    /// </summary>
    public bool IsSelected(string featureName) => _selectedFeatures.Contains(featureName);

    /// <summary>
    /// Gets the index of a feature in the selected features array.
    /// Returns -1 if not found.
    /// </summary>
    public int GetFeatureIndex(string featureName)
    {
        return _featureIndexMap.TryGetValue(featureName, out int index) ? index : -1;
    }

    /// <summary>
    /// Extracts only selected features from TransactionCounters.
    /// </summary>
    public float[] ExtractSelectedFeatures(TransactionCounters counters)
    {
        var features = new float[_orderedFeatures.Length];
        for (int i = 0; i < _orderedFeatures.Length; i++)
        {
            features[i] = counters.GetCounterByName(_orderedFeatures[i]);
        }
        return features;
    }

    /// <summary>
    /// Gets the default path to the configuration file.
    /// </summary>
    private static string GetDefaultConfigPath()
    {
        var assemblyDir = Path.GetDirectoryName(typeof(FeatureConfiguration).Assembly.Location);
        if (!string.IsNullOrEmpty(assemblyDir))
        {
            var configPath = Path.Combine(assemblyDir, "Configuration", "feature_names.json");
            if (File.Exists(configPath))
                return configPath;
        }
        return "Configuration/feature_names.json";
    }

    private class FeatureConfigJson
    {
        public string? description { get; set; }
        public string? version { get; set; }
        public string[]? features { get; set; }
    }
}
