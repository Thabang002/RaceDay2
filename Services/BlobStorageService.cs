using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Services;

/// <summary>
/// Stores images in Azure Blob Storage. The container is private: images are streamed
/// back to clients through <c>GET /api/files/{blobName}</c>, so neither the MVC app nor the
/// browser ever talks to Azure directly.
/// </summary>
public class BlobStorageService : IBlobStorageService
{
    public const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly Dictionary<string, string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
        [".gif"] = "image/gif"
    };

    private static readonly string[] AllowedFolders = { "banners", "profiles" };

    private readonly BlobContainerClient _container;
    private readonly ILogger<BlobStorageService> _logger;
    private bool _containerReady;

    public BlobStorageService(BlobServiceClient serviceClient, IConfiguration config, ILogger<BlobStorageService> logger)
    {
        var containerName = config["AzureStorage:ContainerName"] ?? "raceday-images";
        _container = serviceClient.GetBlobContainerClient(containerName);
        _logger = logger;
    }

    private async Task EnsureContainerAsync(CancellationToken ct)
    {
        if (_containerReady) return;
        await _container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
        _containerReady = true;
    }

    public async Task<string> UploadImageAsync(IFormFile file, string folder, CancellationToken ct = default)
    {
        if (!AllowedFolders.Contains(folder)) throw new ArgumentException("Invalid folder.", nameof(folder));
        if (file == null || file.Length == 0) throw new ImageValidationException("Please choose an image to upload.");
        if (file.Length > MaxBytes) throw new ImageValidationException("Images must be 5 MB or smaller.");

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || !AllowedTypes.TryGetValue(ext, out var contentType))
            throw new ImageValidationException("Only JPG, PNG, WEBP or GIF images are allowed.");

        await EnsureContainerAsync(ct);

        var blobName = $"{folder}/{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var blob = _container.GetBlobClient(blobName);

        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType, CacheControl = "public, max-age=86400" }
        }, ct);

        _logger.LogInformation("Uploaded blob {BlobName} ({Bytes} bytes)", blobName, file.Length);
        return blobName;
    }

    public async Task<(Stream Content, string ContentType)?> DownloadAsync(string blobName, CancellationToken ct = default)
    {
        if (!IsSafeBlobName(blobName)) return null;
        try
        {
            var blob = _container.GetBlobClient(blobName);
            var response = await blob.DownloadStreamingAsync(cancellationToken: ct);
            var type = response.Value.Details.ContentType ?? "application/octet-stream";
            return (response.Value.Content, type);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string? blobName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(blobName) || !IsSafeBlobName(blobName)) return;
        try
        {
            await _container.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: ct);
        }
        catch (RequestFailedException ex)
        {
            // A failed clean-up of an old image must never fail the user's request.
            _logger.LogWarning(ex, "Could not delete blob {BlobName}", blobName);
        }
    }

    private static bool IsSafeBlobName(string blobName)
    {
        if (string.IsNullOrWhiteSpace(blobName) || blobName.Contains("..")) return false;
        var slash = blobName.IndexOf('/');
        return slash > 0 && AllowedFolders.Contains(blobName[..slash]);
    }
}
