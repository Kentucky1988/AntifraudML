namespace FraudDetection.Core.Models;

/// <summary>
/// Training data container with transactions and fraud labels.
/// </summary>
public class TrainingData
{
    public Transaction[] Transactions { get; set; } = Array.Empty<Transaction>();
    public bool[] FraudLabels { get; set; } = Array.Empty<bool>();
}
