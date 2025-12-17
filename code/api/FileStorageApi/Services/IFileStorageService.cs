using FileStorageApi.Models;

namespace FileStorageApi.Services;

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

