using System.Text;
using System.Text.Json;

namespace RaceDay.Web.Infrastructure;

public static class SessionKeys
{
    public const string Token = "RaceDay.Token";
    public const string TokenExpiresUtc = "RaceDay.TokenExpires";
    public const string UserId = "RaceDay.UserId";
    public const string FullName = "RaceDay.FullName";
    public const string Email = "RaceDay.Email";
    public const string Role = "RaceDay.Role";
    public const string ProfilePicture = "RaceDay.ProfilePicture";
}

public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

/// <summary>Strongly-typed access to the logged-in user stored in session.</summary>
public static class UserSession
{
    public static bool IsLoggedIn(this ISession s)
    {
        var token = s.GetString(SessionKeys.Token);
        if (string.IsNullOrEmpty(token)) return false;

        var expires = s.GetString(SessionKeys.TokenExpiresUtc);
        if (long.TryParse(expires, out var ticks) && DateTime.UtcNow >= new DateTime(ticks, DateTimeKind.Utc))
        {
            s.Clear(); // JWT expired -> treat as logged out
            return false;
        }
        return true;
    }

    public static string? Token(this ISession s) => s.GetString(SessionKeys.Token);
    public static int UserId(this ISession s) => s.GetInt32(SessionKeys.UserId) ?? 0;
    public static string FullName(this ISession s) => s.GetString(SessionKeys.FullName) ?? string.Empty;
    public static string Email(this ISession s) => s.GetString(SessionKeys.Email) ?? string.Empty;
    public static string? Role(this ISession s) => s.GetString(SessionKeys.Role);
    public static string? ProfilePicture(this ISession s) => s.GetString(SessionKeys.ProfilePicture);
    public static bool IsOrganiser(this ISession s) => s.Role() == Roles.Organiser;
    public static bool IsParticipant(this ISession s) => s.Role() == Roles.Participant;

    public static void SignIn(this ISession s, string token, Models.UserModel user)
    {
        s.SetString(SessionKeys.Token, token);
        s.SetString(SessionKeys.TokenExpiresUtc, (ReadJwtExpiry(token) ?? DateTime.UtcNow.AddHours(2)).Ticks.ToString());
        s.SetInt32(SessionKeys.UserId, user.UserId);
        s.SetString(SessionKeys.Role, user.Role);
        s.UpdateUser(user);
    }

    public static void UpdateUser(this ISession s, Models.UserModel user)
    {
        s.SetString(SessionKeys.FullName, user.FullName);
        s.SetString(SessionKeys.Email, user.Email);
        if (string.IsNullOrEmpty(user.ProfilePicture)) s.Remove(SessionKeys.ProfilePicture);
        else s.SetString(SessionKeys.ProfilePicture, user.ProfilePicture);
    }

    /// <summary>Where a user lands after login, depending on role.</summary>
    public static string HomeUrl(this ISession s) => s.Role() switch
    {
        Roles.Organiser => "/Organiser/Dashboard",
        Roles.Participant => "/Participant/Events",
        _ => "/"
    };

    /// <summary>Reads the "exp" claim from a JWT payload (no validation - the API validates the token).</summary>
    private static DateTime? ReadJwtExpiry(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2) return null;
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
            return doc.RootElement.TryGetProperty("exp", out var exp) && exp.TryGetInt64(out var secs)
                ? DateTimeOffset.FromUnixTimeSeconds(secs).UtcDateTime
                : null;
        }
        catch
        {
            return null;
        }
    }
}
