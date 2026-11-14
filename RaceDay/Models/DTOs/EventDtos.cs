using System.ComponentModel.DataAnnotations;

namespace RaceDay.Models.DTOs;

// NOTE: DistanceKm and EventType are optional trailing parameters so that existing
// callers (including the Part 2 test project) keep compiling unchanged.
public record CreateEventDto(
    [Required, StringLength(255)] string Title,
    string? Description,
    [Required] DateTime Date,
    [Required] TimeSpan StartTime,
    [Required] int LocationId,
    [Required, Range(1, 100000)] int MaxParticipants,
    [Range(0.1, 1000)] decimal? DistanceKm = null,
    [StringLength(50)] string? EventType = null
);

public record UpdateEventDto(
    [Required, StringLength(255)] string Title,
    string? Description,
    DateTime Date,
    TimeSpan StartTime,
    int LocationId,
    [Range(1, 100000)] int MaxParticipants,
    string? Status,
    [Range(0.1, 1000)] decimal? DistanceKm = null,
    [StringLength(50)] string? EventType = null
);

public record EventDto(
    int EventId, string Title, string? Description,
    DateTime Date, TimeSpan StartTime,
    int LocationId, string? LocationName,
    int MaxParticipants, string Status,
    int CreatedBy, string? OrganiserName,
    DateTime CreatedAt,
    decimal? DistanceKm,
    string? EventType,
    string? BannerImage,
    bool ResultsPublished,
    string? City
);

/// <summary>Organiser dashboard row: an event plus enrolment/category counts.</summary>
public record OrganiserEventSummaryDto(
    int EventId, string Title, DateTime Date, TimeSpan StartTime,
    string? LocationName, decimal? DistanceKm, string? EventType,
    string Status, int MaxParticipants,
    int EnrolmentCount, int ConfirmedCount, int CategoryCount,
    bool ResultsPublished, string? BannerImage
);

public static class EventTypes
{
    public static readonly string[] All =
    {
        "Road Race", "Marathon", "Half Marathon", "Trail Run", "Fun Run",
        "Cycling", "Mountain Bike", "Triathlon", "Duathlon", "Open Water Swim", "Obstacle Race"
    };
}

public static class EventStatuses
{
    public static readonly string[] All = { "Scheduled", "Open", "Closed", "Completed", "Cancelled" };
}
