namespace FileStorageApi.Models;

public class FileEntity
{
    public Guid Id { get; set; }
    public string Filename { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
    public byte[] FileData { get; set; } = Array.Empty<byte>();
}

