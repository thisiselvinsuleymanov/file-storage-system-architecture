using FileStorageApi.Data;
using FileStorageApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FileStorageApi.Services;

public class DatabaseStorageService : IFileStorageService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseStorageService> _logger;

    public DatabaseStorageService(
        ApplicationDbContext context,
        ILogger<DatabaseStorageService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<FileMetadata> UploadAsync(
        Stream fileStream,
        string filename,
        string contentType,
        CancellationToken cancellationToken = default)
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
            FileData = fileData,
            UploadedAt = DateTime.UtcNow
        };

        _context.Files.Add(file);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Uploaded file {FileId} ({Filename}, {Size} bytes) to database storage",
            file.Id, filename, fileData.Length);

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
        var file = await _context.Files
            .FindAsync(new object[] { fileId }, cancellationToken);

        if (file == null)
        {
            _logger.LogWarning("File {FileId} not found in database storage", fileId);
            throw new FileNotFoundException($"File {fileId} not found");
        }

        _logger.LogInformation("Downloaded file {FileId} ({Filename}, {Size} bytes) from database storage",
            file.Id, file.Filename, file.FileSize);

        return new MemoryStream(file.FileData);
    }

    public async Task<FileMetadata> GetMetadataAsync(
        Guid fileId,
        CancellationToken cancellationToken = default)
    {
        var file = await _context.Files
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
        var file = await _context.Files
            .FindAsync(new object[] { fileId }, cancellationToken);

        if (file == null)
        {
            throw new FileNotFoundException($"File {fileId} not found");
        }

        _context.Files.Remove(file);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted file {FileId} ({Filename}) from database storage",
            file.Id, file.Filename);
    }

    public async Task<IEnumerable<FileMetadata>> ListAsync(
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        return await _context.Files
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

