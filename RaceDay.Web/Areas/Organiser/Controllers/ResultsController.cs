using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Areas.Organiser.Controllers;

public class ResultsController : OrganiserControllerBase
{
    public ResultsController(IRaceDayApiClient api) : base(api) { }

    /// <summary>Results-entry sheet for an event.</summary>
    [HttpGet]
    public async Task<IActionResult> Index(int eventId)
    {
        var (ev, failure) = await LoadOwnEventAsync(eventId);
        if (failure != null) return failure;

        var rows = await Api.GetEventResultsAsync(eventId);
        if (!rows.Success) Error(rows.Error ?? "We couldn't load the results sheet.");

        return View(new ResultsEntryViewModel
        {
            Event = ev!,
            Rows = (rows.Data ?? new()).Select(r => new ResultRowInput
            {
                EnrolmentId = r.EnrolmentId,
                ParticipantName = r.ParticipantName,
                EntryNumber = r.EntryNumber,
                CategoryName = r.CategoryName,
                PacePerKm = r.PacePerKm,
                FinishTime = r.FinishTime.HasValue ? FormatTime(r.FinishTime.Value) : null,
                Position = r.Position,
                Status = r.Status,
                Notes = r.Notes
            }).ToList()
        });
    }

    [HttpPost]
    public async Task<IActionResult> Index(int eventId, ResultsEntryViewModel model)
    {
        var (ev, failure) = await LoadOwnEventAsync(eventId);
        if (failure != null) return failure;
        model.Event = ev!;

        if (!model.CanCapture)
        {
            Error($"Results can only be captured once the event has taken place ({ev!.Date:d MMM yyyy}).");
            return RedirectToAction(nameof(Index), new { eventId });
        }

        if (!ModelState.IsValid)
        {
            ModelState.AddModelError(string.Empty, "Please fix the highlighted rows and save again.");
            return View(model);
        }

        var request = new SaveResultsRequest(
            model.Rows.Select(r => new SaveResultRequest(
                r.EnrolmentId,
                ParseTime(r.FinishTime),
                model.AutoRank ? null : r.Position,
                string.IsNullOrWhiteSpace(r.Status) ? null : r.Status,
                string.IsNullOrWhiteSpace(r.Notes) ? null : r.Notes.Trim())).ToList(),
            model.AutoRank);

        var result = await Api.SaveResultsAsync(eventId, request);
        if (!result.Success)
        {
            AddApiErrors(result);
            return View(model);
        }

        Success(model.AutoRank
            ? "Results saved and positions calculated from finish times."
            : "Results saved.");
        return RedirectToAction(nameof(Index), new { eventId });
    }

    [HttpPost]
    public async Task<IActionResult> Publish(int eventId, bool publish = true)
    {
        var result = await Api.PublishResultsAsync(eventId, publish);
        if (result.Success)
            Success(publish ? "Results published! Participants can now see them under My Results." : "Results hidden from participants.");
        else
            Error(result.Error ?? "We couldn't change the publish status.");
        return RedirectToAction(nameof(Index), new { eventId });
    }

    private static string FormatTime(TimeSpan t) =>
        $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}";

    private static TimeSpan? ParseTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var parts = value.Trim().Split(':');
        if (parts.Length == 3 &&
            int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var h) &&
            int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var m) &&
            int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var s))
            return new TimeSpan(h, m, s);
        return null;
    }
}
