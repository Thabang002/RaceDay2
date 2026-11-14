namespace RaceDay.Web.Infrastructure;

/// <summary>
/// Quick client-friendly checks before sending a file to the API (the API re-validates everything).
/// </summary>
public static class ImageRules
{
    public const long MaxBytes = 5 * 1024 * 1024;
    public const string Accept = ".jpg,.jpeg,.png,.webp,.gif";
    private static readonly string[] Extensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    public static string? Check(IFormFile? file)
    {
        if (file == null || file.Length == 0) return "Please choose an image to upload.";
        if (file.Length > MaxBytes) return "Images must be 5 MB or smaller.";
        if (!Extensions.Contains(Path.GetExtension(file.FileName).ToLowerInvariant()))
            return "Only JPG, PNG, WEBP or GIF images are allowed.";
        return null;
    }
}
