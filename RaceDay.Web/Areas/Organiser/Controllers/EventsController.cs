using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RaceDay.Web.Models;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Areas.Organiser.Controllers;

public class EventsController : OrganiserControllerBase
{
    public EventsController(IRaceDayApiClient api) : base(api) { }

    public async Task<IActionResult> Index()
    {
        var result = await Api.GetMyEventsAsync();
        if (!result.Success) Error(result.Error ?? "We couldn't load your events.");
        return View(result.Data ?? new List<OrganiserEventSummary>());
    }

    public async Task<IActionResult> Details(int id)
    {
        var (ev, failure) = await LoadOwnEventAsync(id);
        if (failure != null) return failure;

        var categories = await Api.GetCategoriesAsync(id);
        var enrolments = await Api.GetEventEnrolmentsAsync(id);
        var active = enrolments.Data?.Where(e => e.IsActive).ToList() ?? new();

        return View(new EventManageViewModel
        {
            Event = ev!,
            Categories = categories.Data ?? new(),
            EnrolmentCount = active.Count,
            ConfirmedCount = active.Count(e => e.StatusLabel == "Confirmed")
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? locationId = null)
    {
        var model = new EventFormViewModel
        {
            Date = DateTime.Today.AddDays(30),
            StartTime = new TimeSpan(7, 0, 0),
            LocationId = locationId
        };
        await PopulateListsAsync(model);
        return View("Form", model);
    }

    [HttpPost]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        model.EventId = null;
        if (!ModelState.IsValid)
        {
            await PopulateListsAsync(model);
            return View("Form", model);
        }

        var result = await Api.CreateEventAsync(ToRequest(model));
        if (!result.Success)
        {
            AddApiErrors(result);
            await PopulateListsAsync(model);
            return View("Form", model);
        }

        var eventId = result.Data;
        await TryUploadBannerAsync(eventId, model.Banner);

        Success($"\"{model.Title}\" has been created. Now add the race categories participants can enter.");
        return RedirectToAction("Index", "Categories", new { eventId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var (ev, failure) = await LoadOwnEventAsync(id);
        if (failure != null) return failure;

        var model = new EventFormViewModel
        {
            EventId = ev!.EventId,
            Title = ev.Title,
            Description = ev.Description,
            Date = ev.Date.Date,
            StartTime = ev.StartTime,
            LocationId = ev.LocationId,
            DistanceKm = ev.DistanceKm,
            EventType = ev.EventType,
            MaxParticipants = ev.MaxParticipants,
            Status = ev.Status,
            ExistingBanner = ev.BannerImage
        };
        await PopulateListsAsync(model);
        return View("Form", model);
    }

    [HttpPost]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model)
    {
        model.EventId = id;
        if (!ModelState.IsValid)
        {
            await PopulateListsAsync(model);
            return View("Form", model);
        }

        var result = await Api.UpdateEventAsync(id, ToRequest(model));
        if (!result.Success)
        {
            if (result.IsNotFound) return NotFound();
            if (result.IsForbidden) return RedirectToAction("AccessDenied", "Home", new { area = "" });
            AddApiErrors(result);
            await PopulateListsAsync(model);
            return View("Form", model);
        }

        await TryUploadBannerAsync(id, model.Banner);
        if (TempData["Warning"] == null) Success("Event updated.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await Api.DeleteEventAsync(id);
        if (result.Success) Success("Event deleted.");
        else Error(result.Error ?? "We couldn't delete that event.");
        return RedirectToAction(nameof(Index));
    }

    // ------------------------------------------------------------------

    /// <summary>The event is already saved at this point, so a failed upload is a warning, not a failure.</summary>
    private async Task TryUploadBannerAsync(int eventId, IFormFile? banner)
    {
        if (banner == null || banner.Length == 0) return;
        var upload = await Api.UploadBannerAsync(eventId, banner);
        if (!upload.Success)
            Warning($"The event was saved, but the banner image could not be uploaded: {upload.Error}");
    }

    private async Task PopulateListsAsync(EventFormViewModel model)
    {
        var locations = await Api.GetLocationsAsync();
        var types = await Api.GetEventTypesAsync();

        model.Locations = (locations.Data ?? new())
            .OrderBy(l => l.Name)
            .Select(l => new SelectListItem(l.DisplayName, l.LocationId.ToString(), l.LocationId == model.LocationId))
            .ToList();
        model.EventTypes = types.Data ?? new();

        if (!locations.Success || !types.Success)
            ModelState.AddModelError(string.Empty, "Some form options could not be loaded. Please refresh the page.");
    }

    private static EventRequest ToRequest(EventFormViewModel m) => new(
        m.Title.Trim(), m.Description?.Trim(), m.Date!.Value.Date, m.StartTime!.Value,
        m.LocationId!.Value, m.MaxParticipants!.Value,
        m.IsEdit ? m.Status : null, m.DistanceKm, m.EventType);
}
