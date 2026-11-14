using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Areas.Participant.Controllers;

public class EventsController : ParticipantControllerBase
{
    public EventsController(IRaceDayApiClient api) : base(api) { }

    /// <summary>Participant home: all upcoming events with filters.</summary>
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] EventFilter filter)
    {
        filter.Upcoming = true;
        if (filter.From.HasValue && filter.To.HasValue && filter.From > filter.To)
        {
            (filter.From, filter.To) = (filter.To, filter.From);
        }

        var events = await Api.GetEventsAsync(filter);
        if (!events.Success) Error(events.Error ?? "We couldn't load events right now.");

        var types = await Api.GetEventTypesAsync();
        var locations = await Api.GetLocationsAsync();
        var mine = await Api.GetUserEnrolmentsAsync(CurrentUserId);

        return View(new EventBrowseViewModel
        {
            Filter = filter,
            Events = events.Data ?? new(),
            EventTypes = types.Data ?? new(),
            Locations = (locations.Data ?? new()).OrderBy(l => l.Name)
                .Select(l => new SelectListItem(l.DisplayName, l.LocationId.ToString())).ToList(),
            EnrolledEventIds = (mine.Data ?? new()).Where(e => e.IsActive).Select(e => e.EventId).ToHashSet(),
            ParticipantName = HttpContext.Session.FullName()
        });
    }

    /// <summary>Full event information and the categories that can be entered.</summary>
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var model = await BuildDetailsAsync(id);
        return model == null ? NotFound() : View(model);
    }

    /// <summary>Enrol the logged-in participant in a category. Recorded in the API immediately.</summary>
    [HttpPost]
    public async Task<IActionResult> Enrol(int id, int? categoryId)
    {
        if (categoryId is null or <= 0)
        {
            Error("Please choose a category before enrolling.");
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await Api.EnrolAsync(id, categoryId.Value);
        if (result.Success && result.Data != null)
        {
            Success($"You're in! Entry number {result.Data.EntryNumber} for {result.Data.CategoryName}. " +
                    "Your enrolment is pending until the organiser confirms it.");
            return RedirectToAction("Index", "Enrolments");
        }

        Error(result.Error switch
        {
            "Already enrolled." => "You're already enrolled for this event.",
            "Category is full." => "Sorry, that category is full. Please choose another one.",
            _ => result.Error ?? "We couldn't complete your enrolment. Please try again."
        });
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<EventDetailsViewModel?> BuildDetailsAsync(int id)
    {
        var ev = await Api.GetEventAsync(id);
        if (!ev.Success || ev.Data == null) return null;

        var categories = await Api.GetCategoriesAsync(id);
        var mine = await Api.GetUserEnrolmentsAsync(CurrentUserId);

        return new EventDetailsViewModel
        {
            Event = ev.Data,
            Categories = categories.Data ?? new(),
            MyEnrolment = mine.Data?.FirstOrDefault(e => e.EventId == id && e.IsActive)
        };
    }
}
