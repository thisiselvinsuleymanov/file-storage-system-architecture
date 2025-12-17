using System.Diagnostics;
using System.Text;
using System.Text.Json;
using BenchmarkRunner.Models;
using BenchmarkRunner.Scenarios;

var baseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";

Console.WriteLine($"Running benchmarks against: {baseUrl}");
Console.WriteLine();

var results = new List<BenchmarkResult>();

// Upload benchmarks
await RunUploadBenchmarks(baseUrl, results);

// Download benchmarks
await RunDownloadBenchmarks(baseUrl, results);

// Save results
var resultsJson = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
var resultsPath = Path.Combine("results", $"benchmark-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
Directory.CreateDirectory("results");
await File.WriteAllTextAsync(resultsPath, resultsJson);

Console.WriteLine($"\nResults saved to: {resultsPath}");

static async Task RunUploadBenchmarks(string baseUrl, List<BenchmarkResult> results)
{
    Console.WriteLine("=== Upload Benchmarks ===");
    
    var fileSizes = new[] { 10 * 1024, 100 * 1024, 1024 * 1024, 10 * 1024 * 1024 };
    var fileCount = 10;

    foreach (var fileSize in fileSizes)
    {
        Console.WriteLine($"\nTesting {fileSize / 1024}KB files...");
        
        // Database storage
        var dbResult = await UploadScenario.Run(baseUrl + "/api/files/db", fileSize, fileCount);
        results.Add(dbResult);
        Console.WriteLine($"  Database: {dbResult.AverageLatencyMs:F2}ms avg, {dbResult.ThroughputMBps:F2} MB/s");
        
        // Object storage
        var blobResult = await UploadScenario.Run(baseUrl + "/api/files/blob", fileSize, fileCount);
        results.Add(blobResult);
        Console.WriteLine($"  Object:   {blobResult.AverageLatencyMs:F2}ms avg, {blobResult.ThroughputMBps:F2} MB/s");
    }
}

static async Task RunDownloadBenchmarks(string baseUrl, List<BenchmarkResult> results)
{
    Console.WriteLine("\n=== Download Benchmarks ===");
    Console.WriteLine("(Requires files to be uploaded first)");
    
    // First, upload files to test download
    var httpClient = new HttpClient();
    var fileSizes = new[] { 10 * 1024, 100 * 1024, 1024 * 1024, 10 * 1024 * 1024 };
    var fileCount = 10;
    
    foreach (var fileSize in fileSizes)
    {
        Console.WriteLine($"\nTesting {fileSize / 1024}KB file downloads...");
        
        // Upload files first to get IDs
        var dbFileIds = new List<Guid>();
        var blobFileIds = new List<Guid>();
        
        for (int i = 0; i < fileCount; i++)
        {
            var fileData = GenerateTestFile(fileSize);
            var content = new MultipartFormDataContent
            {
                { new ByteArrayContent(fileData), "file", $"test-{i}.bin" }
            };
            
            // Upload to database storage
            try
            {
                var dbResponse = await httpClient.PostAsync(baseUrl + "/api/files/db/upload", content);
                if (dbResponse.IsSuccessStatusCode)
                {
                    var dbJson = await dbResponse.Content.ReadAsStringAsync();
                    var dbMetadata = JsonSerializer.Deserialize<FileMetadata>(dbJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (dbMetadata != null) dbFileIds.Add(dbMetadata.Id);
                }
            }
            catch { }
            
            // Reset content for blob upload
            content = new MultipartFormDataContent
            {
                { new ByteArrayContent(fileData), "file", $"test-{i}.bin" }
            };
            
            // Upload to object storage
            try
            {
                var blobResponse = await httpClient.PostAsync(baseUrl + "/api/files/blob/upload", content);
                if (blobResponse.IsSuccessStatusCode)
                {
                    var blobJson = await blobResponse.Content.ReadAsStringAsync();
                    var blobMetadata = JsonSerializer.Deserialize<FileMetadata>(blobJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (blobMetadata != null) blobFileIds.Add(blobMetadata.Id);
                }
            }
            catch { }
        }
        
        // Run download benchmarks
        if (dbFileIds.Count >= fileCount)
        {
            var dbResult = await DownloadScenario.Run(baseUrl + "/api/files/db", fileSize, fileCount, dbFileIds);
            results.Add(dbResult);
            Console.WriteLine($"  Database: {dbResult.AverageLatencyMs:F2}ms avg, {dbResult.ThroughputMBps:F2} MB/s");
        }
        
        if (blobFileIds.Count >= fileCount)
        {
            var blobResult = await DownloadScenario.Run(baseUrl + "/api/files/blob", fileSize, fileCount, blobFileIds);
            results.Add(blobResult);
            Console.WriteLine($"  Object:   {blobResult.AverageLatencyMs:F2}ms avg, {blobResult.ThroughputMBps:F2} MB/s");
        }
    }
}

static byte[] GenerateTestFile(int size)
{
    var random = new Random();
    var data = new byte[size];
    random.NextBytes(data);
    return data;
}

class FileMetadata
{
    public Guid Id { get; set; }
    public string Filename { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
}

