using System.Diagnostics;
using FraudDetection.Core.Configuration;
using FraudDetection.Core.Models;
using FraudDetection.Demo;
using FraudDetection.ML;

Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║       Fraud Detection POC - Demo Application                 ║");
Console.WriteLine("║  Isolation Forest + LightGBM Two-Stage Detection System      ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
Console.WriteLine();

// Configuration
const int TrainingDataSize = 1000;
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
    NumberOfLeaves = 31,
    MinimumExampleCountPerLeaf = 20,
    LearningRate = 0.1,
    NumberOfIterations = 100
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
    Console.WriteLine($"   Training IsolationForest completed successfully!");
    Console.WriteLine($"   Time: {stopwatch.ElapsedMilliseconds}ms");
    Console.WriteLine();
    Console.WriteLine("   📈 Training Metrics:");
    Console.WriteLine($"      Accuracy:  {trainingResult.Metrics.Accuracy:P2}");
    Console.WriteLine($"      Precision: {trainingResult.Metrics.Precision:P2}");
    Console.WriteLine($"      Recall:    {trainingResult.Metrics.Recall:P2}");
    Console.WriteLine($"      F1 Score:  {trainingResult.Metrics.F1Score:P2}");
    Console.WriteLine($"      AUC-ROC:   {trainingResult.Metrics.AucRoc:P2}");
    Console.WriteLine();
    Console.WriteLine($"   Models saved to:");
    Console.WriteLine($"      - {trainingResult.IsolationForestModelPath}");
    Console.WriteLine($"      - {trainingResult.LightGbmModelPath}");
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
Console.WriteLine($"   Evaluation completed in {stopwatch.ElapsedMilliseconds}ms");
Console.WriteLine();
Console.WriteLine("   📈 Test Metrics:");
Console.WriteLine($"      Accuracy:  {testMetrics.Accuracy:P2}");
Console.WriteLine($"      Precision: {testMetrics.Precision:P2}");
Console.WriteLine($"      Recall:    {testMetrics.Recall:P2}");
Console.WriteLine($"      F1 Score:  {testMetrics.F1Score:P2}");
Console.WriteLine($"      AUC-ROC:   {testMetrics.AucRoc:P2}");
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

// Step 8: Model persistence demo
Console.WriteLine("💾 Step 8: Model persistence demo...");
Console.WriteLine("   Loading models from disk...");

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

if (loadedPrediction != null)
{
    Console.WriteLine($"   Loaded model prediction for {loadedPrediction.TransactionId}:");
    Console.WriteLine($"      Is Fraud: {loadedPrediction.IsFraudTransaction}");
    Console.WriteLine($"      Confidence: {loadedPrediction.ConfidenceScore:P2}");
    Console.WriteLine($"      Anomaly Score: {loadedPrediction.AnomalyScore:F4}");
}
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
