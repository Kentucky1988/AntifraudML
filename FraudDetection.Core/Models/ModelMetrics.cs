namespace FraudDetection.Core.Models;

/// <summary>
/// Model performance metrics after training.
/// </summary>
public class ModelMetrics
{
    public double Accuracy { get; set; }
    public double Precision { get; set; }
    public double Recall { get; set; }
    
    /// <summary>
    /// F1 Score - harmonic mean of Precision and Recall (balanced).
    /// </summary>
    public double F1Score { get; set; }
    
    /// <summary>
    /// F2 Score - weighted harmonic mean favoring Recall over Precision.
    /// Better for fraud detection where missing fraud (FN) is worse than false alarms (FP).
    /// </summary>
    public double F2Score { get; set; }
    
    /// <summary>
    /// Area Under ROC Curve. Good for balanced datasets.
    /// </summary>
    public double AucRoc { get; set; }
    
    /// <summary>
    /// Area Under Precision-Recall Curve. Better for imbalanced datasets (fraud detection).
    /// </summary>
    public double AucPr { get; set; }
}
