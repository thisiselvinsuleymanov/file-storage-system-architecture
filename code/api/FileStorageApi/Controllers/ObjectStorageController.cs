using FileStorageApi.Models;
using FileStorageApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileStorageApi.Controllers;

[ApiController]
[Route("api/files/blob")]
public class ObjectStorageController : ControllerBase
{
    private readonly ObjectStorageService _storageService;
    private readonly ILogger<ObjectStorageController> _logger;

    public ObjectStorageController(
        ObjectStorageService storageService,
        ILogger<ObjectStorageController> logger)
    {
        _storageService = storageService;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<ActionResult<FileMetadata>> Upload(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file provided");
        }

        try
        {
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to object storage");
            return StatusCode(500, "Error uploading file");
        }
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {FileId} from object storage", id);
            return StatusCode(500, "Error downloading file");
        }
    }

    [HttpGet("{id}/metadata")]
    public async Task<ActionResult<FileMetadata>> GetMetadata(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var metadata = await _storageService.GetMetadataAsync(id, cancellationToken);
            return Ok(metadata);
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metadata for file {FileId} from object storage", id);
            return StatusCode(500, "Error getting file metadata");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _storageService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FileId} from object storage", id);
            return StatusCode(500, "Error deleting file");
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FileMetadata>>> List(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var files = await _storageService.ListAsync(skip, take, cancellationToken);
            return Ok(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing files from object storage");
            return StatusCode(500, "Error listing files");
        }
    }
}

