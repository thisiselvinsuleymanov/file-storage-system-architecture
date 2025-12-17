using System.Diagnostics;
using System.Text;
using BenchmarkRunner.Models;

namespace BenchmarkRunner.Scenarios;

public static class UploadScenario
{
    public static async Task<BenchmarkResult> Run(string endpoint, int fileSizeBytes, int fileCount)
    {
        var httpClient = new HttpClient();
        var latencies = new List<double>();
        var successCount = 0;
        var failureCount = 0;
        var totalBytes = 0L;

        for (int i = 0; i < fileCount; i++)
        {
            var fileData = GenerateTestFile(fileSizeBytes);
            var content = new MultipartFormDataContent
            {
                { new ByteArrayContent(fileData), "file", $"test-{i}.bin" }
            };

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var response = await httpClient.PostAsync(endpoint + "/upload", content);
                stopwatch.Stop();

                if (response.IsSuccessStatusCode)
                {
                    var latency = stopwatch.Elapsed.TotalMilliseconds;
                    latencies.Add(latency);
                    totalBytes += fileSizeBytes;
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
            Operation = "Upload",
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

    private static byte[] GenerateTestFile(int size)
    {
        var random = new Random();
        var data = new byte[size];
        random.NextBytes(data);
        return data;
    }
}

