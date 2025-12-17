namespace BenchmarkRunner.Models;

public class BenchmarkResult
{
    public string Operation { get; set; } = string.Empty;
    public string StorageType { get; set; } = string.Empty;
    public int FileSizeBytes { get; set; }
    public int FileCount { get; set; }
    public double AverageLatencyMs { get; set; }
    public double P95LatencyMs { get; set; }
    public double P99LatencyMs { get; set; }
    public double ThroughputMBps { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

