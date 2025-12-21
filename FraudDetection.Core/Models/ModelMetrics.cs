namespace FraudDetection.Core.Models;

/// <summary>
/// Model performance metrics after training.
/// </summary>
public class ModelMetrics
{
    public double Accuracy { get; set; }
    public double Precision { get; set; }
    public double Recall { get; set; }
    public double F1Score { get; set; }
    public double AucRoc { get; set; }
}
