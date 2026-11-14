using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.Models.DTOs;
using RaceDay.Models.Entities;

namespace RaceDay.Controllers;

[ApiController]
[Route("api")]
[Authorize]
[Tags("Results")]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    public ResultsController(RaceDayDbContext db) => _db = db;

    private int CurrentUserId => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

    /// <summary>Results sheet for an event: every active enrolment with its result (owner Organiser only).</summary>
    [HttpGet("events/{id:int}/results")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> GetEventResults(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null || ev.IsDeleted) return NotFound(new { message = "Event not found." });
        if (ev.CreatedBy != CurrentUserId) return Forbid();

        var rows = await _db.Enrolments
            .Where(e => e.EventId == id && e.Status == EnrolmentStatus.Active)
            .Select(e => new ResultEntryDto(
                e.EnrolmentId, e.UserId, e.User!.FullName, e.EntryNumber,
                e.CategoryId, e.Category!.Name,
                e.Result != null ? e.Result.ResultId : (int?)null,
                e.Result != null ? e.Result.FinishTime : null,
                e.Result != null ? e.Result.Position : null,
                e.Result != null ? e.Result.PacePerKm : null,
                e.Result != null ? e.Result.Status : null,
                e.Result != null ? e.Result.Notes : null))
            .ToListAsync();

        // Ranked finishers first, then everyone else alphabetically.
        var ordered = rows
            .OrderBy(r => r.Position ?? int.MaxValue)
            .ThenBy(r => r.ParticipantName)
            .ToList();

        return Ok(ordered);
    }

    /// <summary>
    /// Capture finish times and positions for an event (owner Organiser only). Only allowed once the event date has passed.
    /// Set <c>autoRank</c> to true to assign positions automatically from finish times.
    /// </summary>
    [HttpPut("events/{id:int}/results")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> SaveResults(int id, [FromBody] SaveResultsDto dto)
    {
        var ev = await _db.Events.Include(e => e.Categories).FirstOrDefaultAsync(e => e.EventId == id);
        if (ev == null || ev.IsDeleted) return NotFound(new { message = "Event not found." });
        if (ev.CreatedBy != CurrentUserId) return Forbid();
        if (ev.Date.Date > DateTime.UtcNow.Date)
            return BadRequest(new { message = "Results can only be captured after the event has taken place." });

        var enrolments = await _db.Enrolments
            .Include(e => e.Result)
            .Where(e => e.EventId == id && e.Status == EnrolmentStatus.Active)
            .ToDictionaryAsync(e => e.EnrolmentId);

        var errors = new List<string>();
        foreach (var r in dto.Results)
        {
            if (!enrolments.ContainsKey(r.EnrolmentId))
                errors.Add($"Enrolment {r.EnrolmentId} does not belong to this event.");
            if (r.FinishTime.HasValue && (r.FinishTime.Value <= TimeSpan.Zero || r.FinishTime.Value > TimeSpan.FromHours(72)))
                errors.Add("Finish times must be greater than zero and less than 72 hours.");
            if (r.Status != null && !ResultStatuses.All.Contains(r.Status))
                errors.Add($"Invalid result status '{r.Status}'.");
        }

        // If the same enrolment appears twice, the last entry wins.
        var rows = dto.Results
            .Where(r => enrolments.ContainsKey(r.EnrolmentId))
            .GroupBy(r => r.EnrolmentId)
            .Select(g => g.Last())
            .ToList();

        // Work out the effective position for each row.
        var positions = rows.ToDictionary(r => r.EnrolmentId, r => r.Position);
        if (dto.AutoRank)
        {
            var rank = 1;
            foreach (var r in rows.Where(IsFinisher).OrderBy(x => x.FinishTime))
                positions[r.EnrolmentId] = rank++;
            foreach (var r in rows.Where(x => !IsFinisher(x)))
                positions[r.EnrolmentId] = null;
        }
        else
        {
            var duplicates = positions.Values.Where(p => p.HasValue).GroupBy(p => p).Where(g => g.Count() > 1).Select(g => g.Key);
            foreach (var d in duplicates) errors.Add($"Position {d} has been given to more than one participant.");
        }

        if (errors.Count > 0)
            return BadRequest(new { message = string.Join(" ", errors.Distinct()) });

        foreach (var r in rows)
        {
            var enrolment = enrolments[r.EnrolmentId];
            var isEmpty = r.FinishTime == null && positions[r.EnrolmentId] == null && string.IsNullOrWhiteSpace(r.Status);

            if (isEmpty)
            {
                if (enrolment.Result != null) _db.Results.Remove(enrolment.Result);
                continue;
            }

            var result = enrolment.Result ?? new Result { EnrolmentId = enrolment.EnrolmentId };
            result.FinishTime = r.FinishTime;
            result.Position = positions[r.EnrolmentId];
            result.Status = r.Status ?? (r.FinishTime.HasValue ? ResultStatuses.Finished : null);
            result.Notes = r.Notes;

            var distance = ev.Categories.FirstOrDefault(c => c.CategoryId == enrolment.CategoryId)?.DistanceKm ?? ev.DistanceKm;
            result.PacePerKm = r.FinishTime.HasValue && distance is > 0m
                ? Math.Round((decimal)r.FinishTime.Value.TotalMinutes / distance.Value, 2)
                : null;

            if (enrolment.Result == null) _db.Results.Add(result);
        }

        await _db.SaveChangesAsync();
        return await GetEventResults(id);
    }

    /// <summary>Publish (or unpublish) results so participants can see them (owner Organiser only).</summary>
    [HttpPost("events/{id:int}/results/publish")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Publish(int id, [FromQuery] bool publish = true)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null || ev.IsDeleted) return NotFound(new { message = "Event not found." });
        if (ev.CreatedBy != CurrentUserId) return Forbid();

        if (publish)
        {
            var hasResults = await _db.Results.AnyAsync(r => r.Enrolment!.EventId == id);
            if (!hasResults) return BadRequest(new { message = "Capture at least one result before publishing." });
            ev.Status = "Completed";
        }

        ev.ResultsPublished = publish;
        await _db.SaveChangesAsync();
        return Ok(new { ev.EventId, ev.ResultsPublished, ev.Status });
    }

    /// <summary>The logged-in participant's race history across events with published results.</summary>
    [HttpGet("users/me/results")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> MyResults()
    {
        var uid = CurrentUserId;

        var mine = await _db.Enrolments
            .Where(e => e.UserId == uid && e.Result != null && e.Event!.ResultsPublished && !e.Event.IsDeleted)
            .Select(e => new
            {
                e.EventId,
                e.Event!.Title,
                e.Event.Date,
                e.Event.EventType,
                CategoryName = e.Category!.Name,
                CategoryDistance = e.Category.DistanceKm,
                EventDistance = e.Event.DistanceKm,
                e.Result!.FinishTime,
                e.Result.Position,
                e.Result.PacePerKm,
                e.Result.Status
            })
            .ToListAsync();

        var eventIds = mine.Select(m => m.EventId).Distinct().ToList();

        // "Finishers" = results in the event with a finish time and a position.
        var finisherCounts = await _db.Results
            .Where(r => eventIds.Contains(r.Enrolment!.EventId) && r.FinishTime != null && r.Position != null)
            .GroupBy(r => r.Enrolment!.EventId)
            .Select(g => new { EventId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EventId, x => x.Count);

        var list = mine
            .OrderByDescending(m => m.Date)
            .Select(m => new MyResultDto(
                m.EventId, m.Title, m.Date, m.EventType, m.CategoryName,
                m.CategoryDistance > 0 ? m.CategoryDistance : m.EventDistance,
                m.FinishTime, m.Position,
                finisherCounts.TryGetValue(m.EventId, out var c) ? c : 0,
                m.PacePerKm, m.Status))
            .ToList();

        return Ok(list);
    }

    private static bool IsFinisher(SaveResultDto r) =>
        r.FinishTime.HasValue && (r.Status == null || r.Status == ResultStatuses.Finished);
}
