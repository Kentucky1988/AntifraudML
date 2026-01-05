namespace FraudDetection.Core.Models;

/// <summary>
/// Training data container with labeled transactions.
/// </summary>
public class TrainingData
{
    public Transaction[] Transactions { get; set; } = Array.Empty<Transaction>();
}
