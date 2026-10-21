namespace RaceDay.Models.DTOs;

public record CreateResultDto(
    TimeSpan? FinishTime,
    int? Position,
    decimal? PacePerKm,
    string? Status,
    string? Notes
);

public record UpdateResultDto(
    TimeSpan? FinishTime,
    int? Position,
    decimal? PacePerKm,
    string? Status,
    string? Notes
);

public record ResultDto(
    int ResultId, int EnrolmentId,
    string? ParticipantName, string? EventTitle, string? CategoryName,
    TimeSpan? FinishTime, int? Position, decimal? PacePerKm,
    string? Status, string? Notes
);