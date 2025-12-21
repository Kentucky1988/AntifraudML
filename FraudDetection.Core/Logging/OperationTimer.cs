using System.Diagnostics;
using FraudDetection.Core.Interfaces;

namespace FraudDetection.Core.Logging;

/// <summary>
/// Helper class for timing operations and automatically logging latency.
/// Implements IDisposable for use with 'using' statements.
/// </summary>
public class OperationTimer : IDisposable
{
    private readonly IFraudLogger _logger;
    private readonly string _operationName;
    private readonly Stopwatch _stopwatch;
    private bool _disposed;

    /// <summary>
    /// Creates a new OperationTimer that starts timing immediately.
    /// </summary>
    /// <param name="logger">The logger to write latency metrics to</param>
    /// <param name="operationName">Name of the operation being timed</param>
    public OperationTimer(IFraudLogger logger, string operationName)
    {
        _logger = logger;
        _operationName = operationName;
        _stopwatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// Gets the elapsed time in milliseconds.
    /// </summary>
    public double ElapsedMs => _stopwatch.Elapsed.TotalMilliseconds;

    /// <summary>
    /// Stops the timer and logs the latency.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        
        _stopwatch.Stop();
        _logger.LogLatency(_operationName, _stopwatch.Elapsed.TotalMilliseconds);
        _disposed = true;
    }
}

/// <summary>
/// Extension methods for IFraudLogger.
/// </summary>
public static class FraudLoggerExtensions
{
    /// <summary>
    /// Creates an OperationTimer for timing an operation.
    /// Use with 'using' statement for automatic logging on completion.
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="operationName">Name of the operation to time</param>
    /// <returns>An OperationTimer that logs latency when disposed</returns>
    public static OperationTimer TimeOperation(this IFraudLogger logger, string operationName)
    {
        return new OperationTimer(logger, operationName);
    }
}
