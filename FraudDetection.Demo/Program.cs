using System.Diagnostics;
using FraudDetection.Core.Configuration;
using FraudDetection.Core.Models;
using FraudDetection.Demo;
using FraudDetection.ML;

await RunDemoAsync();

async Task RunDemoAsync()
{
Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║       Fraud Detection POC - Demo Application                 ║");
Console.WriteLine("║  Isolation Forest + LightGBM Two-Stage Detection System      ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
Console.WriteLine();

// Configuration
const int TrainingDataSize = 10_000;
const int TestDataSize = 200;
const float FraudRate = 0.1f;
const string IsolationForestModelPath = "models/isolation_forest.onnx";
const string LightGbmModelPath = "models/lightgbm.onnx";

// Ensure models directory exists
Directory.CreateDirectory("models");

var stopwatch = new Stopwatch();

// Step 1: Generate training data
Console.WriteLine("📊 Step 1: Generating synthetic training data...");
stopwatch.Start();
var trainingData = SampleDataGenerator.GenerateTrainingData(TrainingDataSize, FraudRate);
stopwatch.Stop();
Console.WriteLine($"   Generated {TrainingDataSize} transactions ({(int)(FraudRate * 100)}% fraud rate)");
Console.WriteLine($"   Time: {stopwatch.ElapsedMilliseconds}ms");
Console.WriteLine();

// Step 2: Initialize components
Console.WriteLine("🔧 Step 2: Initializing ML components...");
var isolationForestConfig = new IsolationForestConfig
{
    NumberOfTrees = 100,
    SampleSize = 256,
    ContaminationRate = FraudRate,
    AnomalyThreshold = 0.5f
};

var lightGbmConfig = new LightGbmConfig
{
    NumberOfLeaves = 63,
    MinimumExampleCountPerLeaf = 20,
    LearningRate = 0.05,
    NumberOfIterations = 500
};

var featureExtractor = new FeatureExtractor();
var anomalyDetector = new OnnxIsolationForestDetector(isolationForestConfig);
var fraudClassifier = new OnnxLightGbmClassifier(lightGbmConfig);
var trainingPipeline = new TrainingPipeline(featureExtractor, anomalyDetector, fraudClassifier);
trainingPipeline.SetModelPaths(IsolationForestModelPath, LightGbmModelPath);
Console.WriteLine("   Components initialized successfully");
Console.WriteLine();

// Step 3: Train models
Console.WriteLine("🎓 Step 3: Training models...");
stopwatch.Restart();
var trainingResult = trainingPipeline.Train(trainingData);
stopwatch.Stop();

if (trainingResult.Success)
{
    Console.WriteLine($"   Training completed successfully!");
    Console.WriteLine($"   Time: {stopwatch.ElapsedMilliseconds}ms");
    Console.WriteLine();
    Console.WriteLine("   📈 Training Metrics:");
    Console.WriteLine($"      Accuracy:  {trainingResult.Metrics.Accuracy:P2}");
    Console.WriteLine($"      Precision: {trainingResult.Metrics.Precision:P2}");
    Console.WriteLine($"      Recall:    {trainingResult.Metrics.Recall:P2}");
    Console.WriteLine($"      F1 Score:  {trainingResult.Metrics.F1Score:P2}");
    Console.WriteLine($"      F2 Score:  {trainingResult.Metrics.F2Score:P2}");
    Console.WriteLine($"      AUC-ROC:   {trainingResult.Metrics.AucRoc:P2}");
    Console.WriteLine($"      AUC-PR:    {trainingResult.Metrics.AucPr:P2}");
    Console.WriteLine();
    
    // Show model sizes
    var ifSize = new FileInfo(trainingResult.IsolationForestModelPath).Length;
    var lgbSize = new FileInfo(trainingResult.LightGbmModelPath).Length;
    Console.WriteLine($"   💾 Models saved:");
    Console.WriteLine($"      - {trainingResult.IsolationForestModelPath} ({ifSize / 1024.0:F1} KB)");
    Console.WriteLine($"      - {trainingResult.LightGbmModelPath} ({lgbSize / 1024.0:F1} KB)");
}
else
{
    Console.WriteLine("   ❌ Training failed!");
    return;
}
Console.WriteLine();

// Step 4: Generate test data
Console.WriteLine("📊 Step 4: Generating test data...");
var testData = SampleDataGenerator.GenerateTrainingData(TestDataSize, FraudRate);
Console.WriteLine($"   Generated {TestDataSize} test transactions");
Console.WriteLine();

// Step 5: Evaluate on test data
Console.WriteLine("📊 Step 5: Evaluating on test data...");
stopwatch.Restart();
var testMetrics = trainingPipeline.Evaluate(testData);
stopwatch.Stop();
double avgEvalLatency = (double)stopwatch.ElapsedMilliseconds / TestDataSize;
Console.WriteLine($"   Evaluation completed in {stopwatch.ElapsedMilliseconds}ms");
Console.WriteLine($"   Average latency: {avgEvalLatency:F2}ms per transaction");
Console.WriteLine();
Console.WriteLine("   📈 Test Metrics:");
Console.WriteLine($"      Accuracy:  {testMetrics.Accuracy:P2}");
Console.WriteLine($"      Precision: {testMetrics.Precision:P2}");
Console.WriteLine($"      Recall:    {testMetrics.Recall:P2}");
Console.WriteLine($"      F1 Score:  {testMetrics.F1Score:P2}");
Console.WriteLine($"      F2 Score:  {testMetrics.F2Score:P2}");
Console.WriteLine($"      AUC-ROC:   {testMetrics.AucRoc:P2}");
Console.WriteLine($"      AUC-PR:    {testMetrics.AucPr:P2}");
Console.WriteLine();

// Step 6: Run inference pipeline demo
Console.WriteLine("🔍 Step 6: Running inference pipeline demo...");
var inferencePipeline = new InferencePipeline(featureExtractor, anomalyDetector, fraudClassifier);

// Generate some sample transactions for demo
Console.WriteLine();
Console.WriteLine("   Sample Predictions:");
Console.WriteLine("   ─────────────────────────────────────────────────────────────");

var sampleTransactions = new[]
{
    SampleDataGenerator.GenerateTransaction("DEMO-LEGIT-001", isFraud: false),
    SampleDataGenerator.GenerateTransaction("DEMO-LEGIT-002", isFraud: false),
    SampleDataGenerator.GenerateTransaction("DEMO-FRAUD-001", isFraud: true),
    SampleDataGenerator.GenerateTransaction("DEMO-FRAUD-002", isFraud: true),
    SampleDataGenerator.GenerateTransaction("DEMO-LEGIT-003", isFraud: false),
};

foreach (var transaction in sampleTransactions)
{
    stopwatch.Restart();
    var prediction = inferencePipeline.Predict(transaction);
    stopwatch.Stop();

    if (prediction != null)
    {
        var fraudIndicator = prediction.IsFraudTransaction ? "🚨 FRAUD" : "✅ LEGIT";
        var expectedType = transaction.TransactionId.Contains("FRAUD") ? "Expected: FRAUD" : "Expected: LEGIT";
        
        Console.WriteLine($"   {prediction.TransactionId}:");
        Console.WriteLine($"      Result: {fraudIndicator}");
        Console.WriteLine($"      Confidence: {prediction.ConfidenceScore:P2}");
        Console.WriteLine($"      Anomaly Score: {prediction.AnomalyScore:F4}");
        Console.WriteLine($"      Amount: €{transaction.Amount:N2}");
        Console.WriteLine($"      Latency: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"      ({expectedType})");
        Console.WriteLine();
    }
}

// Step 7: Batch prediction performance test
Console.WriteLine("⚡ Step 7: Batch prediction performance test...");
const int BatchSize = 100;
var batchRandom = new Random(123);
var batchTransactions = new Transaction[BatchSize];
for (int i = 0; i < BatchSize; i++)
{
    batchTransactions[i] = SampleDataGenerator.GenerateTransaction($"BATCH-{i:D4}", batchRandom.NextDouble() < FraudRate);
}

stopwatch.Restart();
var batchPredictions = inferencePipeline.PredictBatch(batchTransactions);
stopwatch.Stop();

int fraudCount = batchPredictions.Count(p => p?.IsFraudTransaction == true);
int legitCount = batchPredictions.Count(p => p?.IsFraudTransaction == false);
double avgLatency = (double)stopwatch.ElapsedMilliseconds / BatchSize;

Console.WriteLine($"   Processed {BatchSize} transactions");
Console.WriteLine($"   Total time: {stopwatch.ElapsedMilliseconds}ms");
Console.WriteLine($"   Average latency: {avgLatency:F2}ms per transaction");
Console.WriteLine($"   Detected: {fraudCount} fraud, {legitCount} legitimate");
Console.WriteLine();

// Step 8: Model persistence demo (restart)
Console.WriteLine("💾 Step 8: Model persistence demo...");
Console.WriteLine("   Loading models from disk...");
stopwatch.Restart();

var loadedAnomalyDetector = new OnnxIsolationForestDetector(isolationForestConfig);
var loadedFraudClassifier = new OnnxLightGbmClassifier(lightGbmConfig);

loadedAnomalyDetector.LoadModel(IsolationForestModelPath);
loadedFraudClassifier.LoadModel(LightGbmModelPath);

// Create new inference pipeline with loaded models
var loadedFeatureExtractor = new FeatureExtractor();
// Note: Normalization is now handled by ONNX models (sklearn Pipeline with MinMaxScaler)

var loadedInferencePipeline = new InferencePipeline(loadedFeatureExtractor, loadedAnomalyDetector, loadedFraudClassifier);

// Test with a sample transaction
var testTransaction = SampleDataGenerator.GenerateTransaction("PERSISTENCE-TEST", isFraud: true);
var loadedPrediction = loadedInferencePipeline.Predict(testTransaction);

var loadTime = stopwatch.ElapsedMilliseconds;
Console.WriteLine($"   Model load + prediction time: {loadTime}ms");

if (loadedPrediction != null)
{
    Console.WriteLine($"   Loaded model prediction for {loadedPrediction.TransactionId}:");
    Console.WriteLine($"      Is Fraud: {loadedPrediction.IsFraudTransaction}");
    Console.WriteLine($"      Confidence: {loadedPrediction.ConfidenceScore:P2}");
    Console.WriteLine($"      Anomaly Score: {loadedPrediction.AnomalyScore:F4}");
    
    // Generate SHAP explanation if fraud detected
    if (loadedPrediction.IsFraudTransaction)
    {
        Console.WriteLine();
        Console.WriteLine("   🔍 TreeSHAP Explanation (why flagged as fraud):");
        
        string lgbModelPath = LightGbmModelPath.Replace(".onnx", ".lgb.txt");
        if (File.Exists(lgbModelPath))
        {
            // Get combined features for explanation
            var fv = loadedFeatureExtractor.Extract(testTransaction);
            if (fv != null)
            {
                var anomalyResult = loadedAnomalyDetector.Predict(fv);
                var combinedFeatures = new float[fv.Features.Length + 1];
                Array.Copy(fv.Features, combinedFeatures, fv.Features.Length);
                combinedFeatures[^1] = anomalyResult.AnomalyScore;
                
                var explainStopwatch = Stopwatch.StartNew();
                var explanation = await loadedFraudClassifier.ExplainTransactionAsync(combinedFeatures, lgbModelPath);
                explainStopwatch.Stop();
                
                if (explanation != null)
                {
                    Console.WriteLine($"      Explanation time: {explainStopwatch.ElapsedMilliseconds}ms");
                    
                    // Parse and display top contributors
                    try
                    {
                        var json = System.Text.Json.JsonDocument.Parse(explanation);
                        var topContributors = json.RootElement.GetProperty("top_contributors");
                        
                        // Get feature names for mapping
                        var featureNames = FraudDetection.Core.Models.DepositCounter.AllCounterNames;
                        
                        Console.WriteLine("      Top contributing features:");
                        int shown = 0;
                        foreach (var contributor in topContributors.EnumerateArray())
                        {
                            if (shown >= 5) break;
                            var feature = contributor.GetProperty("feature").GetString() ?? "";
                            var shapValue = contributor.GetProperty("shap_value").GetDouble();
                            var value = contributor.GetProperty("value").GetDouble();
                            var sign = shapValue > 0 ? "+" : "";
                            
                            // Map f0, f1, etc. to actual counter names
                            string displayName = feature;
                            if (feature.StartsWith("f") && int.TryParse(feature[1..], out int idx))
                            {
                                if (idx < featureNames.Length)
                                    displayName = featureNames[idx];
                                else if (idx == featureNames.Length)
                                    displayName = "anomaly_score";
                            }
                            
                            Console.WriteLine($"         {sign}{shapValue:F4}  {displayName} = {value:F2}");
                            shown++;
                        }
                        
                        var summary = json.RootElement.GetProperty("summary").GetString();
                        Console.WriteLine($"      Summary: {summary}");
                    }
                    catch
                    {
                        Console.WriteLine($"      Raw explanation: {explanation[..Math.Min(200, explanation.Length)]}...");
                    }
                }
                else
                {
                    Console.WriteLine("      SHAP explanation generation failed.");
                }
            }
        }
        else
        {
            Console.WriteLine($"      LightGBM model file not found: {lgbModelPath}");
            Console.WriteLine("      (SHAP explanations require .lgb.txt model file)");
        }
    }
}
stopwatch.Stop();
Console.WriteLine($"   Total time (load + predict + explain): {stopwatch.ElapsedMilliseconds}ms");
Console.WriteLine();

// Summary
Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║                    Demo Complete!                            ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
Console.WriteLine();
Console.WriteLine("Summary:");
Console.WriteLine($"  • Trained on {TrainingDataSize} transactions");
Console.WriteLine($"  • Test accuracy: {testMetrics.Accuracy:P2}");
Console.WriteLine($"  • Average inference latency: {avgLatency:F2}ms");
Console.WriteLine($"  • Models saved to ./models/");
Console.WriteLine();
Console.WriteLine("The fraud detection system is ready for production use!");
}
