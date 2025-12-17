using System.Diagnostics;
using System.Text.Json;
using BenchmarkRunner.Models;

namespace BenchmarkRunner.Scenarios;

public static class DownloadScenario
{
    public static async Task<BenchmarkResult> Run(string endpoint, int fileSizeBytes, int fileCount, List<Guid> fileIds)
    {
        if (fileIds.Count < fileCount)
        {
            throw new ArgumentException($"Need at least {fileCount} file IDs, but got {fileIds.Count}");
        }

        var httpClient = new HttpClient();
        var latencies = new List<double>();
        var successCount = 0;
        var failureCount = 0;
        var totalBytes = 0L;

        for (int i = 0; i < fileCount; i++)
        {
            var fileId = fileIds[i];
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var response = await httpClient.GetAsync($"{endpoint}/{fileId}");
                stopwatch.Stop();

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsByteArrayAsync();
                    var latency = stopwatch.Elapsed.TotalMilliseconds;
                    latencies.Add(latency);
                    totalBytes += content.Length;
                    successCount++;
                }
                else
                {
                    failureCount++;
                }
            }
            catch
            {
                failureCount++;
            }
        }

        latencies.Sort();
        var avgLatency = latencies.Any() ? latencies.Average() : 0;
        var p95Latency = latencies.Count > 0 ? latencies[(int)(latencies.Count * 0.95)] : 0;
        var p99Latency = latencies.Count > 0 ? latencies[(int)(latencies.Count * 0.99)] : 0;
        var totalSeconds = latencies.Sum() / 1000.0;
        var throughputMBps = totalSeconds > 0 ? (totalBytes / (1024.0 * 1024.0)) / totalSeconds : 0;

        return new BenchmarkResult
        {
            Operation = "Download",
            StorageType = endpoint.Contains("/db") ? "Database" : "Object",
            FileSizeBytes = fileSizeBytes,
            FileCount = fileCount,
            AverageLatencyMs = avgLatency,
            P95LatencyMs = p95Latency,
            P99LatencyMs = p99Latency,
            ThroughputMBps = throughputMBps,
            SuccessCount = successCount,
            FailureCount = failureCount
        };
    }
}

