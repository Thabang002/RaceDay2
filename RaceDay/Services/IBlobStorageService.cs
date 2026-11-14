namespace RaceDay.Services;

public interface IBlobStorageService
{
    /// <summary>Uploads an image and returns the blob name (e.g. "banners/3f2a….jpg").</summary>
    Task<string> UploadImageAsync(IFormFile file, string folder, CancellationToken ct = default);

    /// <summary>Opens a blob for streaming. Returns null when it does not exist.</summary>
    Task<(Stream Content, string ContentType)?> DownloadAsync(string blobName, CancellationToken ct = default);

    Task DeleteAsync(string? blobName, CancellationToken ct = default);
}

public class ImageValidationException : Exception
{
    public ImageValidationException(string message) : base(message) { }
}
