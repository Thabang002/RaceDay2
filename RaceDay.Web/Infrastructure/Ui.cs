using System.Globalization;

namespace RaceDay.Web.Infrastructure;

/// <summary>Small presentation helpers shared by all views so every page formats things the same way.</summary>
public static class Ui
{
    /// <summary>
    /// en-GB formatting ("." decimals, "9 Nov 2026" dates) so output is identical on Windows and in the Linux container.
    /// </summary>
    public static readonly CultureInfo Culture = LoadCulture();

    private static CultureInfo LoadCulture()
    {
        try { return CultureInfo.GetCultureInfo("en-GB"); }
        catch (CultureNotFoundException) { return CultureInfo.InvariantCulture; }
    }

    /// <summary>Image URL served by this app's /media proxy (which streams from the API).</summary>
    public static string? MediaUrl(string? blobName) =>
        string.IsNullOrWhiteSpace(blobName) ? null : "/media/" + blobName;

    /// <summary>CSS class for a status badge. Green = Confirmed, amber = Pending, etc.</summary>
    public static string StatusClass(string? status) => (status ?? string.Empty).ToLowerInvariant() switch
    {
        "confirmed" or "paid" or "finished" or "completed" => "rd-status-confirmed",
        "pending" or "scheduled" => "rd-status-pending",
        "open" or "active" => "rd-status-open",
        "withdrawn" or "cancelled" or "dnf" or "dsq" => "rd-status-cancelled",
        "closed" or "dns" => "rd-status-muted",
        _ => "rd-status-muted"
    };

    public static string StatusIcon(string? status) => (status ?? string.Empty).ToLowerInvariant() switch
    {
        "confirmed" or "paid" or "finished" or "completed" => "bi-check-circle-fill",
        "pending" or "scheduled" => "bi-hourglass-split",
        "open" or "active" => "bi-door-open-fill",
        "withdrawn" or "cancelled" or "dnf" or "dsq" => "bi-x-circle-fill",
        _ => "bi-dash-circle"
    };

    public static string EventTypeIcon(string? type) => type switch
    {
        "Cycling" or "Mountain Bike" => "bi-bicycle",
        "Open Water Swim" => "bi-water",
        "Triathlon" or "Duathlon" => "bi-trophy-fill",
        "Trail Run" => "bi-tree-fill",
        "Obstacle Race" => "bi-bricks",
        "Fun Run" => "bi-emoji-smile-fill",
        _ => "bi-lightning-charge-fill"
    };

    public static string Ordinal(int n)
    {
        var rem100 = n % 100;
        var suffix = rem100 is >= 11 and <= 13 ? "th" : (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
        return n.ToString("N0", Culture) + suffix;
    }

    public static string Time(TimeSpan? t) =>
        t.HasValue ? $"{(int)t.Value.TotalHours}:{t.Value.Minutes:00}:{t.Value.Seconds:00}" : "—";

    public static string Clock(TimeSpan t) => DateTime.Today.Add(t).ToString("HH:mm", Culture);

    public static string Pace(decimal? minutesPerKm)
    {
        if (!minutesPerKm.HasValue || minutesPerKm <= 0) return "—";
        var total = (int)Math.Round(minutesPerKm.Value * 60);
        return $"{total / 60}:{total % 60:00} /km";
    }

    public static string Km(decimal? km) =>
        km.HasValue ? km.Value.ToString("0.##", Culture) + " km" : "—";

    public static string Money(decimal amount) =>
        amount == 0 ? "Free" : "R " + amount.ToString("N2", Culture);

    public static string Date(DateTime d) => d.ToString("ddd, d MMM yyyy", Culture);
    public static string ShortDate(DateTime d) => d.ToString("d MMM yyyy", Culture);

    public static string Initials(string? name)
    {
        var parts = (name ?? "?").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "?",
            1 => parts[0][..1].ToUpperInvariant(),
            _ => (parts[0][..1] + parts[^1][..1]).ToUpperInvariant()
        };
    }

    public static string DaysToGo(DateTime date)
    {
        var days = (date.Date - DateTime.Today).Days;
        return days switch
        {
            < 0 => "Finished",
            0 => "Race day!",
            1 => "Tomorrow",
            _ => $"{days} days to go"
        };
    }
}
