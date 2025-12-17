using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FileStorageApi.Data;
using FileStorageApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FileStorageApi.Services;

public class ObjectStorageService : IFileStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ObjectStorageService> _logger;
    private const string ContainerName = "files";

    public ObjectStorageService(
        BlobServiceClient blobServiceClient,
        ApplicationDbContext context,
        ILogger<ObjectStorageService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _context = context;
        _logger = logger;
    }

    public async Task<FileMetadata> UploadAsync(
        Stream fileStream,
        string filename,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        // Ensure container exists
        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
        await containerClient.CreateIfNotExistsAsync(
            PublicAccessType.None,
            cancellationToken: cancellationToken);

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

        _logger.LogInformation("Uploaded file {FileId} ({Filename}, {Size} bytes) to object storage",
            file.Id, filename, fileSize);

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
        CancellationToken cancellationToken = default)
    {
        // Get metadata from database
        var file = await _context.FilesBlob
            .FindAsync(new object[] { fileId }, cancellationToken);

        if (file == null)
        {
            _logger.LogWarning("File {FileId} not found in object storage metadata", fileId);
            throw new FileNotFoundException($"File {fileId} not found");
        }

        // Download from blob storage
        var containerClient = _blobServiceClient.GetBlobContainerClient(file.BlobContainer);
        var blobClient = containerClient.GetBlobClient(file.BlobName);

        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            _logger.LogWarning("Blob {BlobName} not found in container {Container} for file {FileId}",
                file.BlobName, file.BlobContainer, fileId);
            throw new FileNotFoundException($"Blob for file {fileId} not found");
        }

        var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);

        _logger.LogInformation("Downloaded file {FileId} ({Filename}, {Size} bytes) from object storage",
            file.Id, file.Filename, file.FileSize);

        return response.Value.Content;
    }

    public async Task<FileMetadata> GetMetadataAsync(
        Guid fileId,
        CancellationToken cancellationToken = default)
    {
        var file = await _context.FilesBlob
            .Where(f => f.Id == fileId)
            .Select(f => new FileMetadata
            {
                Id = f.Id,
                Filename = f.Filename,
                ContentType = f.ContentType,
                FileSize = f.FileSize,
                UploadedAt = f.UploadedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (file == null)
        {
            throw new FileNotFoundException($"File {fileId} not found");
        }

        return file;
    }

    public async Task DeleteAsync(
        Guid fileId,
        CancellationToken cancellationToken = default)
    {
        var file = await _context.FilesBlob
            .FindAsync(new object[] { fileId }, cancellationToken);

        if (file == null)
        {
            throw new FileNotFoundException($"File {fileId} not found");
        }

        // Delete from blob storage
        var containerClient = _blobServiceClient.GetBlobContainerClient(file.BlobContainer);
        var blobClient = containerClient.GetBlobClient(file.BlobName);

        try
        {
            await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete blob {BlobName} for file {FileId}",
                file.BlobName, fileId);
            // Continue to delete metadata even if blob deletion fails
        }

        // Delete metadata from database
        _context.FilesBlob.Remove(file);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted file {FileId} ({Filename}) from object storage",
            file.Id, file.Filename);
    }

    public async Task<IEnumerable<FileMetadata>> ListAsync(
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        return await _context.FilesBlob
            .OrderByDescending(f => f.UploadedAt)
            .Skip(skip)
            .Take(take)
            .Select(f => new FileMetadata
            {
                Id = f.Id,
                Filename = f.Filename,
                ContentType = f.ContentType,
                FileSize = f.FileSize,
                UploadedAt = f.UploadedAt
            })
            .ToListAsync(cancellationToken);
    }
}

