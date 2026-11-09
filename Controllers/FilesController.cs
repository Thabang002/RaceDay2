using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Services;

namespace RaceDay.Controllers;

/// <summary>
/// Streams images out of the private Azure Blob Storage container.
/// Clients only ever see API URLs, never Azure URLs or keys.
/// </summary>
[ApiController]
[Route("api/files")]
[Tags("Files")]
public class FilesController : ControllerBase
{
    private readonly IBlobStorageService _blobs;
    public FilesController(IBlobStorageService blobs) => _blobs = blobs;

    /// <summary>Download an image by blob name, e.g. <c>banners/3f2a….jpg</c>.</summary>
    [HttpGet("{**blobName}")]
    [AllowAnonymous]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Get(string blobName, CancellationToken ct)
    {
        var file = await _blobs.DownloadAsync(blobName, ct);
        if (file == null) return NotFound();
        return File(file.Value.Content, file.Value.ContentType);
    }
}
