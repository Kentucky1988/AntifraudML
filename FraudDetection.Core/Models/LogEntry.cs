using System.Text.Json.Serialization;

namespace FraudDetection.Core.Models;

/// <summary>
/// Base class for all log entries with common fields.
/// </summary>
public abstract class LogEntry
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("logType")]
    public abstract string LogType { get; }
}

/// <summary>
/// Log entry for processed transactions.
/// </summary>
public class TransactionLogEntry : LogEntry
{
    [JsonPropertyName("logType")]
    public override string LogType => "Transaction";

    [JsonPropertyName("transactionId")]
    public string TransactionId { get; set; } = string.Empty;

    [JsonPropertyName("anomalyScore")]
    public float AnomalyScore { get; set; }

    [JsonPropertyName("isFraudTransaction")]
    public bool IsFraudTransaction { get; set; }

    [JsonPropertyName("latencyMs")]
    public double LatencyMs { get; set; }
}

/// <summary>
/// Log entry for errors.
/// </summary>
public class ErrorLogEntry : LogEntry
{
    [JsonPropertyName("logType")]
    public override string LogType => "Error";

    [JsonPropertyName("errorMessage")]
    public string ErrorMessage { get; set; } = string.Empty;

    [JsonPropertyName("stackTrace")]
    public string? StackTrace { get; set; }

    [JsonPropertyName("context")]
    public string Context { get; set; } = string.Empty;

    [JsonPropertyName("exceptionType")]
    public string ExceptionType { get; set; } = string.Empty;
}

/// <summary>
/// Log entry for latency metrics.
/// </summary>
public class LatencyLogEntry : LogEntry
{
    [JsonPropertyName("logType")]
    public override string LogType => "Latency";

    [JsonPropertyName("operationName")]
    public string OperationName { get; set; } = string.Empty;

    [JsonPropertyName("latencyMs")]
    public double LatencyMs { get; set; }
}
