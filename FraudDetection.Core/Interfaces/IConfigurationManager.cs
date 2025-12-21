using FraudDetection.Core.Configuration;

namespace FraudDetection.Core.Interfaces;

/// <summary>
/// Manages configuration for ML models.
/// </summary>
public interface IConfigurationManager
{
    /// <summary>
    /// Gets the Isolation Forest configuration.
    /// </summary>
    IsolationForestConfig GetIsolationForestConfig();
    
    /// <summary>
    /// Gets the LightGBM configuration.
    /// </summary>
    LightGbmConfig GetLightGbmConfig();
    
    /// <summary>
    /// Validates all configuration parameters.
    /// </summary>
    void Validate();
}
