using System.Text.Json.Serialization;

namespace FraudDetection.Core.Models;

/// <summary>
/// Contains all counters for transaction analysis.
/// Counters are stored as JSON in PostgreSQL and deserialized into this dictionary.
/// </summary>
public class TransactionCounters
{
    /// <summary>
    /// All counter values keyed by counter name (from DepositCounter constants).
    /// Values are stored as float for ML compatibility.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object?> RawCounters { get; set; } = new();

    /// <summary>
    /// Gets a counter value by name, returning 0 if not found or null.
    /// </summary>
    public float GetCounter(string counterName)
    {
        if (RawCounters.TryGetValue(counterName, out var value) && value != null)
        {
            return Convert.ToSingle(value);
        }
        return 0f;
    }

    /// <summary>
    /// Sets a counter value by name.
    /// </summary>
    public void SetCounter(string counterName, float value)
    {
        RawCounters[counterName] = value;
    }

    /// <summary>
    /// Extracts all counters in the order defined by DepositCounter.AllCounterNames.
    /// Missing counters default to 0.
    /// </summary>
    public float[] ToFeatureArray()
    {
        var features = new float[DepositCounter.TotalCounterCount];
        for (int i = 0; i < DepositCounter.AllCounterNames.Length; i++)
        {
            features[i] = GetCounter(DepositCounter.AllCounterNames[i]);
        }
        return features;
    }
}
