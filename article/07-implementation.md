# 7. Implementation

This section provides a detailed walkthrough of the implementation, including code structure, key components, and design patterns.

## 7.1 Project Structure

```
code/
├── api/
│   ├── FileStorageApi/
│   │   ├── Controllers/
│   │   ├── Services/
│   │   ├── Models/
│   │   ├── Data/
│   │   └── Program.cs
│   └── FileStorageApi.csproj
├── database/
│   ├── schema.sql
│   ├── migrations/
│   └── init.sql
├── storage/
│   └── azurite/
│       └── docker-compose.azurite.yml
└── benchmarks/
    ├── BenchmarkRunner/
    │   ├── Scenarios/
    │   ├── Collectors/
    │   └── Program.cs
    └── BenchmarkRunner.csproj
```

## 7.2 API Implementation

### 7.2.1 Service Abstraction

The core abstraction allows switching between storage strategies:

```csharp
public interface IFileStorageService
{
    Task<FileMetadata> UploadAsync(
        Stream fileStream, 
        string filename, 
        string contentType,
        CancellationToken cancellationToken = default);
    
    Task<Stream> DownloadAsync(
        Guid fileId, 
        CancellationToken cancellationToken = default);
    
    Task<FileMetadata> GetMetadataAsync(
        Guid fileId, 
        CancellationToken cancellationToken = default);
    
    Task DeleteAsync(
        Guid fileId, 
        CancellationToken cancellationToken = default);
    
    Task<IEnumerable<FileMetadata>> ListAsync(
        int skip = 0, 
        int take = 100,
        CancellationToken cancellationToken = default);
}
```

### 7.2.2 Database Storage Implementation

**Service Implementation:**

```csharp
public class DatabaseStorageService : IFileStorageService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseStorageService> _logger;

    public async Task<FileMetadata> UploadAsync(
        Stream fileStream, 
        string filename, 
        string contentType,
        CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream, cancellationToken);
        var fileData = memoryStream.ToArray();

        var file = new FileEntity
        {
            Id = Guid.NewGuid(),
            Filename = filename,
            ContentType = contentType,
            FileSize = fileData.Length,
            FileData = fileData,  // Stored in BYTEA column
            UploadedAt = DateTime.UtcNow
        };

        _context.Files.Add(file);
        await _context.SaveChangesAsync(cancellationToken);

        return new FileMetadata
        {
            Id = file.Id,
            Filename = file.Filename,
            ContentType = file.ContentType,
            FileSize = file.FileSize,
            UploadedAt = file.UploadedAt
        };
    }

    public async Task<Stream> DownloadAsync(
        Guid fileId, 
        CancellationToken cancellationToken)
    {
        var file = await _context.Files
            .FindAsync(new object[] { fileId }, cancellationToken);
        
        if (file == null)
            throw new FileNotFoundException($"File {fileId} not found");

        return new MemoryStream(file.FileData);
    }

    // ... other methods
}
```

**Entity Model:**

```csharp
public class FileEntity
{
    public Guid Id { get; set; }
    public string Filename { get; set; }
    public string ContentType { get; set; }
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
    public byte[] FileData { get; set; }  // BYTEA in PostgreSQL
}
```

**DbContext Configuration:**

```csharp
public class ApplicationDbContext : DbContext
{
    public DbSet<FileEntity> Files { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FileEntity>(entity =>
        {
            entity.ToTable("files_db");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Filename).HasMaxLength(255);
            entity.Property(e => e.ContentType).HasMaxLength(100);
            entity.Property(e => e.FileData)
                .HasColumnType("bytea");  // PostgreSQL BYTEA type
        });
    }
}
```

### 7.2.3 Object Storage Implementation

**Service Implementation:**

```csharp
public class ObjectStorageService : IFileStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ObjectStorageService> _logger;
    private const string ContainerName = "files";

    public async Task<FileMetadata> UploadAsync(
        Stream fileStream, 
        string filename, 
        string contentType,
        CancellationToken cancellationToken)
    {
        // Ensure container exists
        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        // Generate unique blob name
        var blobName = $"{Guid.NewGuid()}/{filename}";
        var blobClient = containerClient.GetBlobClient(blobName);

        // Upload to blob storage
        await blobClient.UploadAsync(
            fileStream, 
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                }
            },
            cancellationToken);

        // Get file size
        var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
        var fileSize = properties.Value.ContentLength;

        // Store metadata in database
        var file = new FileBlobEntity
        {
            Id = Guid.NewGuid(),
            Filename = filename,
            ContentType = contentType,
            FileSize = fileSize,
            BlobContainer = ContainerName,
            BlobName = blobName,
            UploadedAt = DateTime.UtcNow
        };

        _context.FilesBlob.Add(file);
        await _context.SaveChangesAsync(cancellationToken);

        return new FileMetadata
        {
            Id = file.Id,
            Filename = file.Filename,
            ContentType = file.ContentType,
            FileSize = file.FileSize,
            UploadedAt = file.UploadedAt
        };
    }

    public async Task<Stream> DownloadAsync(
        Guid fileId, 
        CancellationToken cancellationToken)
    {
        // Get metadata from database
        var file = await _context.FilesBlob
            .FindAsync(new object[] { fileId }, cancellationToken);
        
        if (file == null)
            throw new FileNotFoundException($"File {fileId} not found");

        // Download from blob storage
        var containerClient = _blobServiceClient.GetBlobContainerClient(file.BlobContainer);
        var blobClient = containerClient.GetBlobClient(file.BlobName);
        
        var response = await blobClient.DownloadStreamingAsync(cancellationToken);
        return response.Value.Content;
    }

    // ... other methods
}
```

**Entity Model:**

```csharp
public class FileBlobEntity
{
    public Guid Id { get; set; }
    public string Filename { get; set; }
    public string ContentType { get; set; }
    public long FileSize { get; set; }
    public string BlobContainer { get; set; }
    public string BlobName { get; set; }
    public DateTime UploadedAt { get; set; }
}
```

### 7.2.4 Controllers

**Database Storage Controller:**

```csharp
[ApiController]
[Route("api/files/db")]
public class DatabaseStorageController : ControllerBase
{
    private readonly DatabaseStorageService _storageService;
    private readonly ILogger<DatabaseStorageController> _logger;

    [HttpPost("upload")]
    public async Task<ActionResult<FileMetadata>> Upload(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided");

        using var stream = file.OpenReadStream();
        var metadata = await _storageService.UploadAsync(
            stream, 
            file.FileName, 
            file.ContentType,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetMetadata), 
            new { id = metadata.Id }, 
            metadata);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Download(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var metadata = await _storageService.GetMetadataAsync(id, cancellationToken);
            var stream = await _storageService.DownloadAsync(id, cancellationToken);

            return File(stream, metadata.ContentType, metadata.Filename);
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
    }

    // ... other endpoints
}
```

**Object Storage Controller:**

Similar structure but using `ObjectStorageService` and route `/api/files/blob`.

### 7.2.5 Dependency Injection

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL")));

// Blob Storage
builder.Services.AddSingleton(serviceProvider =>
{
    var connectionString = builder.Configuration.GetConnectionString("Azurite");
    return new BlobServiceClient(connectionString);
});

// Storage Services
builder.Services.AddScoped<DatabaseStorageService>();
builder.Services.AddScoped<ObjectStorageService>();

// Controllers
builder.Services.AddControllers();
```

## 7.3 Database Schema

### 7.3.1 Database Storage Table

```sql
CREATE TABLE files_db (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    filename VARCHAR(255) NOT NULL,
    content_type VARCHAR(100) NOT NULL,
    file_size BIGINT NOT NULL,
    uploaded_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    file_data BYTEA NOT NULL
);

CREATE INDEX idx_files_db_uploaded_at ON files_db(uploaded_at);
CREATE INDEX idx_files_db_filename ON files_db(filename);
```

### 7.3.2 Object Storage Metadata Table

```sql
CREATE TABLE files_blob (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    filename VARCHAR(255) NOT NULL,
    content_type VARCHAR(100) NOT NULL,
    file_size BIGINT NOT NULL,
    uploaded_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    blob_container VARCHAR(100) NOT NULL,
    blob_name VARCHAR(500) NOT NULL,
    UNIQUE(blob_container, blob_name)
);

CREATE INDEX idx_files_blob_uploaded_at ON files_blob(uploaded_at);
CREATE INDEX idx_files_blob_filename ON files_blob(filename);
CREATE INDEX idx_files_blob_container_name ON files_blob(blob_container, blob_name);
```

## 7.4 Benchmark Implementation

### 7.4.1 Benchmark Runner

```csharp
public class BenchmarkRunner
{
    private readonly HttpClient _httpClient;
    private readonly MetricsCollector _metricsCollector;

    public async Task<BenchmarkResult> RunUploadBenchmark(
        string storageType,
        int fileCount,
        int fileSizeBytes)
    {
        var results = new List<OperationResult>();

        for (int i = 0; i < fileCount; i++)
        {
            var fileData = GenerateTestFile(fileSizeBytes);
            var startTime = DateTime.UtcNow;

            try
            {
                var response = await UploadFile(storageType, fileData);
                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

                results.Add(new OperationResult
                {
                    Success = response.IsSuccessStatusCode,
                    DurationMs = duration,
                    FileSize = fileSizeBytes
                });
            }
            catch (Exception ex)
            {
                results.Add(new OperationResult
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        return new BenchmarkResult
        {
            Operation = "Upload",
            StorageType = storageType,
            Results = results,
            Summary = CalculateSummary(results)
        };
    }

    // ... other benchmark methods
}
```

### 7.4.2 Metrics Collection

```csharp
public class MetricsCollector
{
    public async Task<SystemMetrics> CollectMetricsAsync()
    {
        var metrics = new SystemMetrics
        {
            Timestamp = DateTime.UtcNow,
            CpuUsage = await GetCpuUsageAsync(),
            MemoryUsage = await GetMemoryUsageAsync(),
            DiskIo = await GetDiskIoAsync(),
            DatabaseSize = await GetDatabaseSizeAsync(),
            WalSize = await GetWalSizeAsync()
        };

        return metrics;
    }

    // ... metric collection methods
}
```

## 7.5 Error Handling

### 7.5.1 Retry Logic

```csharp
public class RetryPolicy
{
    public static async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        int maxRetries = 3,
        TimeSpan? delay = null)
    {
        delay ??= TimeSpan.FromSeconds(1);
        var exceptions = new List<Exception>();

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                exceptions.Add(ex);
                await Task.Delay(delay.Value * (attempt + 1)); // Exponential backoff
            }
        }

        throw new AggregateException("Operation failed after retries", exceptions);
    }
}
```

### 7.5.2 Error Responses

```csharp
public class ErrorResponse
{
    public string Error { get; set; }
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }
    public string RequestId { get; set; }
}

// Global exception handler
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>();
        var response = new ErrorResponse
        {
            Error = exception.Error.GetType().Name,
            Message = exception.Error.Message,
            Timestamp = DateTime.UtcNow,
            RequestId = context.TraceIdentifier
        };

        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(response);
    });
});
```

## 7.6 Configuration

### 7.3.1 appsettings.json

```json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=postgres;Port=5432;Database=filestorage;Username=postgres;Password=postgres",
    "Azurite": "UseDevelopmentStorage=true;DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCzY4Ityrq/K1SZFPTOtr/KBHBeksoGMw==;BlobEndpoint=http://azurite:10000/devstoreaccount1;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

## 7.7 Summary

The implementation provides:
- **Clean Abstraction**: Strategy pattern for storage services
- **Type Safety**: Strong typing with Entity Framework
- **Error Handling**: Comprehensive error handling and retry logic
- **Observability**: Logging and metrics collection
- **Testability**: Dependency injection and interfaces

This foundation enables fair comparison between storage strategies while maintaining code quality and maintainability.

