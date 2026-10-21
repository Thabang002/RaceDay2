using System.ComponentModel.DataAnnotations;

namespace RaceDay.Models.DTOs;

public record CreateCategoryDto(
    [Required] string Name,
    string? Description,
    [Required] decimal DistanceKm,
    [Required] decimal StartFee,
    [Required] int MaxParticipants
);

public record UpdateCategoryDto(
    string Name,
    string? Description,
    decimal DistanceKm,
    decimal StartFee,
    int MaxParticipants
);

public record CategoryDto(
    int CategoryId, int EventId, string Name, string? Description,
    decimal DistanceKm, decimal StartFee, int MaxParticipants
);