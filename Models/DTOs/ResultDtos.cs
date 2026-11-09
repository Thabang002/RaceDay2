using System.ComponentModel.DataAnnotations;

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

/// <summary>One row on the organiser's results-entry sheet (every active enrolment, with or without a result).</summary>
public record ResultEntryDto(
    int EnrolmentId, int UserId, string? ParticipantName, string? EntryNumber,
    int CategoryId, string? CategoryName,
    int? ResultId, TimeSpan? FinishTime, int? Position, decimal? PacePerKm,
    string? Status, string? Notes
);

public record SaveResultDto(
    [Required] int EnrolmentId,
    TimeSpan? FinishTime,
    [Range(1, 100000)] int? Position,
    [StringLength(50)] string? Status,
    [StringLength(500)] string? Notes
);

public record SaveResultsDto(
    [Required] List<SaveResultDto> Results,
    bool AutoRank = false
);

/// <summary>A participant's personal race history row.</summary>
public record MyResultDto(
    int EventId, string EventTitle, DateTime EventDate,
    string? EventType, string? CategoryName, decimal? DistanceKm,
    TimeSpan? FinishTime, int? Position, int TotalFinishers,
    decimal? PacePerKm, string? Status
);

public static class ResultStatuses
{
    public const string Finished = "Finished";
    public static readonly string[] All = { "Finished", "DNF", "DNS", "DSQ" };
}
