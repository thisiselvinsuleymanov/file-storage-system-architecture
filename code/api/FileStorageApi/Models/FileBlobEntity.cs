namespace FileStorageApi.Models;

public class FileBlobEntity
{
    public Guid Id { get; set; }
    public string Filename { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
    public string BlobContainer { get; set; } = string.Empty;
    public string BlobName { get; set; } = string.Empty;
}

