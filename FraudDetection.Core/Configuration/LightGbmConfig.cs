namespace FraudDetection.Core.Configuration;

/// <summary>
/// Configuration for LightGBM fraud classifier.
/// </summary>
public class LightGbmConfig
{
    /// <summary>
    /// Maximum number of leaves in one tree. Range: 31-63.
    /// </summary>
    public int NumberOfLeaves { get; set; } = 31;
    
    /// <summary>
    /// Minimum number of samples required in a leaf.
    /// </summary>
    public int MinimumExampleCountPerLeaf { get; set; } = 20;
    
    /// <summary>
    /// Learning rate (shrinkage). Lower = slower but more accurate.
    /// </summary>
    public double LearningRate { get; set; } = 0.1;
    
    /// <summary>
    /// Number of boosting iterations. Range: 100-300.
    /// </summary>
    public int NumberOfIterations { get; set; } = 100;
    
    /// <summary>
    /// L2 regularization (lambda). Prevents overfitting by penalizing large weights.
    /// Higher values = stronger regularization. Default: 0.1
    /// </summary>
    public double L2Regularization { get; set; } = 0.1; //0.1 - 10.0
    
    /// <summary>
    /// Fraction of data to use for each tree (0.0-1.0). 
    /// 0.8 means each tree sees 80% of data. Reduces overfitting.
    /// </summary>
    public double BaggingFraction { get; set; } = 0.8; //0.7 - 0.9
    
    /// <summary>
    /// Frequency of bagging. 1 = every iteration. 0 = disabled.
    /// </summary>
    public int BaggingFreq { get; set; } = 1;
}
