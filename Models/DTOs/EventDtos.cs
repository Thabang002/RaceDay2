using System.ComponentModel.DataAnnotations;

namespace RaceDay.Models.DTOs;

public record CreateEventDto(
    [Required] string Title,
    string? Description,
    [Required] DateTime Date,
    [Required] TimeSpan StartTime,
    [Required] int LocationId,
    [Required] int MaxParticipants
);

public record UpdateEventDto(
    string Title,
    string? Description,
    DateTime Date,
    TimeSpan StartTime,
    int LocationId,
    int MaxParticipants,
    string? Status
);

public record EventDto(
    int EventId, string Title, string? Description,
    DateTime Date, TimeSpan StartTime,
    int LocationId, string? LocationName,
    int MaxParticipants, string Status,
    int CreatedBy, string? OrganiserName,
    DateTime CreatedAt
);