using System.Text.Json;
using FraudDetection.Core.Interfaces;
using FraudDetection.Core.Models;

namespace FraudDetection.Core.Logging;

/// <summary>
/// Implementation of IFraudLogger that outputs structured JSON logs.
/// </summary>
public class FraudLogger : IFraudLogger
{
    private readonly TextWriter _output;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Creates a new FraudLogger that writes to the specified output.
    /// </summary>
    /// <param name="output">The TextWriter to write logs to. Defaults to Console.Out if null.</param>
    public FraudLogger(TextWriter? output = null)
    {
        _output = output ?? Console.Out;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <inheritdoc />
    public void LogTransaction(string transactionId, float anomalyScore, bool isFraud, double latencyMs)
    {
        var entry = new TransactionLogEntry
        {
            TransactionId = transactionId,
            AnomalyScore = anomalyScore,
            IsFraudTransaction = isFraud,
            LatencyMs = latencyMs
        };

        WriteLog(entry);
    }

    /// <inheritdoc />
    public void LogError(Exception exception, string context)
    {
        var entry = new ErrorLogEntry
        {
            ErrorMessage = exception.Message,
            StackTrace = exception.StackTrace,
            Context = context,
            ExceptionType = exception.GetType().FullName ?? exception.GetType().Name
        };

        WriteLog(entry);
    }

    /// <inheritdoc />
    public void LogLatency(string operationName, double latencyMs)
    {
        var entry = new LatencyLogEntry
        {
            OperationName = operationName,
            LatencyMs = latencyMs
        };

        WriteLog(entry);
    }

    private void WriteLog(LogEntry entry)
    {
        var json = JsonSerializer.Serialize(entry, entry.GetType(), _jsonOptions);
        _output.WriteLine(json);
    }
}
