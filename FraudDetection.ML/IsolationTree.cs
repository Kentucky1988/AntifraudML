using System.Text.Json.Serialization;

namespace FraudDetection.ML;

/// <summary>
/// A single Isolation Tree used in the Isolation Forest algorithm.
/// Recursively partitions data by randomly selecting features and split values.
/// </summary>
[Serializable]
public class IsolationTree
{
    /// <summary>
    /// Root node of the tree.
    /// </summary>
    public IsolationTreeNode? Root { get; set; }

    /// <summary>
    /// Maximum depth limit for the tree (log2(sample_size)).
    /// </summary>
    public int MaxDepth { get; set; }

    private readonly Random _random;

    /// <summary>
    /// Creates a new IsolationTree with optional seed for reproducibility.
    /// </summary>
    public IsolationTree(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    /// <summary>
    /// Parameterless constructor for serialization.
    /// </summary>
    public IsolationTree() : this(null) { }

    /// <summary>
    /// Builds the isolation tree from a sample of data.
    /// </summary>
    /// <param name="data">Sample data to build tree from.</param>
    /// <param name="maxDepth">Maximum tree depth (typically log2(sample_size)).</param>
    public void Build(float[][] data, int maxDepth)
    {
        MaxDepth = maxDepth;
        Root = BuildRecursive(data, 0, maxDepth);
    }

    private IsolationTreeNode BuildRecursive(float[][] data, int currentDepth, int maxDepth)
    {
        int n = data.Length;

        // Terminal conditions: max depth reached or single sample
        if (currentDepth >= maxDepth || n <= 1)
        {
            return new IsolationTreeNode
            {
                IsExternal = true,
                Size = n
            };
        }

        // Get number of features from first sample
        int numFeatures = data[0].Length;
        if (numFeatures == 0)
        {
            return new IsolationTreeNode
            {
                IsExternal = true,
                Size = n
            };
        }

        // Randomly select a feature to split on
        int splitFeature = _random.Next(numFeatures);

        // Find min and max values for the selected feature
        float minVal = float.MaxValue;
        float maxVal = float.MinValue;
        foreach (var sample in data)
        {
            if (sample[splitFeature] < minVal) minVal = sample[splitFeature];
            if (sample[splitFeature] > maxVal) maxVal = sample[splitFeature];
        }

        // If all values are the same, create external node
        if (Math.Abs(maxVal - minVal) < float.Epsilon)
        {
            return new IsolationTreeNode
            {
                IsExternal = true,
                Size = n
            };
        }

        // Random split value between min and max
        float splitValue = minVal + (float)_random.NextDouble() * (maxVal - minVal);

        // Partition data
        var leftData = new List<float[]>();
        var rightData = new List<float[]>();

        foreach (var sample in data)
        {
            if (sample[splitFeature] < splitValue)
                leftData.Add(sample);
            else
                rightData.Add(sample);
        }

        // Handle edge case where all data goes to one side
        if (leftData.Count == 0 || rightData.Count == 0)
        {
            return new IsolationTreeNode
            {
                IsExternal = true,
                Size = n
            };
        }

        // Create internal node
        var node = new IsolationTreeNode
        {
            IsExternal = false,
            SplitFeature = splitFeature,
            SplitValue = splitValue,
            Left = BuildRecursive(leftData.ToArray(), currentDepth + 1, maxDepth),
            Right = BuildRecursive(rightData.ToArray(), currentDepth + 1, maxDepth)
        };

        return node;
    }

    /// <summary>
    /// Computes the path length for a sample through the tree.
    /// </summary>
    /// <param name="sample">Feature vector to evaluate.</param>
    /// <returns>Path length (depth at which sample is isolated).</returns>
    public double PathLength(float[] sample)
    {
        if (Root == null)
            throw new InvalidOperationException("Tree has not been built.");

        return PathLengthRecursive(sample, Root, 0);
    }

    private double PathLengthRecursive(float[] sample, IsolationTreeNode node, int currentDepth)
    {
        if (node.IsExternal)
        {
            // Add adjustment for external node size (average path length for remaining samples)
            return currentDepth + AveragePathLength(node.Size);
        }

        // Traverse left or right based on split
        if (sample[node.SplitFeature] < node.SplitValue)
            return PathLengthRecursive(sample, node.Left!, currentDepth + 1);
        else
            return PathLengthRecursive(sample, node.Right!, currentDepth + 1);
    }

    /// <summary>
    /// Computes the average path length c(n) for a BST with n samples.
    /// Used for normalizing anomaly scores.
    /// Formula: c(n) = 2 * H(n-1) - (2*(n-1)/n) where H(i) is harmonic number
    /// </summary>
    public static double AveragePathLength(int n)
    {
        if (n <= 1) return 0;
        if (n == 2) return 1;

        // H(n-1) approximation using Euler's constant
        double harmonicNumber = Math.Log(n - 1) + 0.5772156649;
        return 2.0 * harmonicNumber - (2.0 * (n - 1) / n);
    }
}

/// <summary>
/// Node in an Isolation Tree.
/// </summary>
[Serializable]
public class IsolationTreeNode
{
    /// <summary>
    /// Whether this is an external (leaf) node.
    /// </summary>
    public bool IsExternal { get; set; }

    /// <summary>
    /// For external nodes: number of samples that reached this node.
    /// </summary>
    public int Size { get; set; }

    /// <summary>
    /// For internal nodes: feature index used for splitting.
    /// </summary>
    public int SplitFeature { get; set; }

    /// <summary>
    /// For internal nodes: value used for splitting.
    /// </summary>
    public float SplitValue { get; set; }

    /// <summary>
    /// Left child (samples with feature value < split value).
    /// </summary>
    public IsolationTreeNode? Left { get; set; }

    /// <summary>
    /// Right child (samples with feature value >= split value).
    /// </summary>
    public IsolationTreeNode? Right { get; set; }
}
