namespace RaceDay.Web.Models;

// Client-side copies of the RaceDay API contracts (JSON is camelCase; System.Text.Json web defaults).
// These are plain data holders - no business logic lives in the MVC app.

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public UserModel User { get; set; } = new();
}

public class UserModel
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? City { get; set; }
    public string? ProfilePicture { get; set; }
}

public class EventModel
{
    public int EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public int LocationId { get; set; }
    public string? LocationName { get; set; }
    public string? City { get; set; }
    public int MaxParticipants { get; set; }
    public string Status { get; set; } = string.Empty;
    public int CreatedBy { get; set; }
    public string? OrganiserName { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal? DistanceKm { get; set; }
    public string? EventType { get; set; }
    public string? BannerImage { get; set; }
    public bool ResultsPublished { get; set; }

    public DateTime StartsAt => Date.Date + StartTime;
    public bool IsUpcoming => Date.Date >= DateTime.Today;
}

public class OrganiserEventSummary
{
    public int EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public string? LocationName { get; set; }
    public decimal? DistanceKm { get; set; }
    public string? EventType { get; set; }
    public string Status { get; set; } = string.Empty;
    public int MaxParticipants { get; set; }
    public int EnrolmentCount { get; set; }
    public int ConfirmedCount { get; set; }
    public int CategoryCount { get; set; }
    public bool ResultsPublished { get; set; }
    public string? BannerImage { get; set; }

    public bool IsUpcoming => Date.Date >= DateTime.Today;
    public int FillPercent => MaxParticipants <= 0 ? 0 : Math.Min(100, (int)Math.Round(EnrolmentCount * 100.0 / MaxParticipants));
}

public class CategoryModel
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal DistanceKm { get; set; }
    public decimal StartFee { get; set; }
    public int MaxParticipants { get; set; }
    public int EnrolledCount { get; set; }

    public int SpotsLeft => Math.Max(0, MaxParticipants - EnrolledCount);
    public bool IsFull => SpotsLeft == 0;
}

public class LocationModel
{
    public int LocationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public string DisplayName => string.IsNullOrWhiteSpace(City) ? Name : $"{Name}, {City}";
}

public class EnrolmentModel
{
    public int EnrolmentId { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public int EventId { get; set; }
    public string? EventTitle { get; set; }
    public DateTime? EventDate { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public DateTime RegistrationDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PaymentStatus { get; set; }
    public string? EntryNumber { get; set; }
    public string? DisplayStatus { get; set; }

    public string StatusLabel => DisplayStatus ?? Status;
    public bool IsActive => Status == "Active";
    public bool IsUpcoming => EventDate.HasValue && EventDate.Value.Date >= DateTime.Today;
}

public class ResultEntryModel
{
    public int EnrolmentId { get; set; }
    public int UserId { get; set; }
    public string? ParticipantName { get; set; }
    public string? EntryNumber { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int? ResultId { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public decimal? PacePerKm { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
}

public class MyResultModel
{
    public int EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string? EventType { get; set; }
    public string? CategoryName { get; set; }
    public decimal? DistanceKm { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public int TotalFinishers { get; set; }
    public decimal? PacePerKm { get; set; }
    public string? Status { get; set; }
}

// ----- request bodies sent to the API -----

public record LoginRequest(string Email, string Password);
public record RegisterRequest(string Email, string Password, string FullName, string Role);
public record EventRequest(string Title, string? Description, DateTime Date, TimeSpan StartTime,
    int LocationId, int MaxParticipants, string? Status, decimal? DistanceKm, string? EventType);
public record CategoryRequest(string Name, string? Description, decimal DistanceKm, decimal StartFee, int MaxParticipants);
public record LocationRequest(string Name, string? Address, string? City, string? Province, string? PostalCode,
    decimal? Latitude, decimal? Longitude);
public record EnrolRequest(int CategoryId, string? PaymentStatus);
public record PaymentRequest(string PaymentStatus);
public record ProfileRequest(string FullName, string Email, string? Password, string? PhoneNumber,
    DateTime? DateOfBirth, string? Gender, string? City);
public record SaveResultRequest(int EnrolmentId, TimeSpan? FinishTime, int? Position, string? Status, string? Notes);
public record SaveResultsRequest(List<SaveResultRequest> Results, bool AutoRank);

public class EventFilter
{
    public string? Search { get; set; }
    public string? EventType { get; set; }
    public int? LocationId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? Distance { get; set; }   // preset key, e.g. "5-10"
    public bool Upcoming { get; set; } = true;

    public static readonly (string Key, string Label, decimal? Min, decimal? Max)[] DistancePresets =
    {
        ("", "Any distance", null, null),
        ("0-5", "Up to 5 km", null, 5m),
        ("5-10", "5 – 10 km", 5m, 10m),
        ("10-21.1", "10 – 21.1 km", 10m, 21.1m),
        ("21.1-42.2", "21.1 – 42.2 km", 21.1m, 42.2m),
        ("42.2+", "Ultra (42.2 km+)", 42.2m, null)
    };

    public bool HasFilters => !string.IsNullOrWhiteSpace(Search) || !string.IsNullOrWhiteSpace(EventType)
        || LocationId.HasValue || From.HasValue || To.HasValue || !string.IsNullOrWhiteSpace(Distance);
}
