using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>
/// Serves images (event banners, profile pictures) by proxying them from the API.
/// The browser and the MVC app never talk to Azure Blob Storage directly.
/// </summary>
public partial class MediaController : Controller
{
    private readonly IRaceDayApiClient _api;
    public MediaController(IRaceDayApiClient api) => _api = api;

    [HttpGet("media/{**blobName}")]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Get(string blobName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(blobName) || !SafeName().IsMatch(blobName)) return NotFound();

        var response = await _api.GetFileAsync(blobName, ct);
        if (response == null) return NotFound();

        HttpContext.Response.RegisterForDispose(response);
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        var stream = await response.Content.ReadAsStreamAsync(ct);
        return File(stream, contentType);
    }

    [GeneratedRegex(@"^(banners|profiles)/[A-Za-z0-9\-]+\.(jpg|jpeg|png|webp|gif)$")]
    private static partial Regex SafeName();
}
