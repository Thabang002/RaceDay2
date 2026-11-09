using System.ComponentModel.DataAnnotations;

namespace RaceDay.Models.DTOs;

public record CreateCategoryDto(
    [Required, StringLength(255)] string Name,
    string? Description,
    [Required, Range(0, 1000)] decimal DistanceKm,
    [Required, Range(0, 100000)] decimal StartFee,
    [Required, Range(1, 100000)] int MaxParticipants
);

public record UpdateCategoryDto(
    [Required, StringLength(255)] string Name,
    string? Description,
    [Range(0, 1000)] decimal DistanceKm,
    [Range(0, 100000)] decimal StartFee,
    [Range(1, 100000)] int MaxParticipants
);

public record CategoryDto(
    int CategoryId, int EventId, string Name, string? Description,
    decimal DistanceKm, decimal StartFee, int MaxParticipants,
    int EnrolledCount
);
