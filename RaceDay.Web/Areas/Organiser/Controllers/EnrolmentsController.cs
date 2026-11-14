using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Areas.Organiser.Controllers;

public class EnrolmentsController : OrganiserControllerBase
{
    public EnrolmentsController(IRaceDayApiClient api) : base(api) { }

    /// <summary>Everyone enrolled for an event, with category and status.</summary>
    public async Task<IActionResult> Index(int eventId, int? category = null, string? status = null)
    {
        var (ev, failure) = await LoadOwnEventAsync(eventId);
        if (failure != null) return failure;

        var enrolments = await Api.GetEventEnrolmentsAsync(eventId);
        if (!enrolments.Success) Error(enrolments.Error ?? "We couldn't load the enrolments.");
        var categories = await Api.GetCategoriesAsync(eventId);

        return View(new EnrolmentsPageViewModel
        {
            Event = ev!,
            Enrolments = enrolments.Data ?? new(),
            Categories = categories.Data ?? new(),
            CategoryFilter = category,
            StatusFilter = status
        });
    }

    /// <summary>Confirm (mark paid) or set an enrolment back to pending.</summary>
    [HttpPost]
    public async Task<IActionResult> SetStatus(int id, int eventId, bool confirm)
    {
        var result = await Api.UpdatePaymentAsync(id, confirm ? "Paid" : "Pending");
        if (result.Success) Success(confirm ? "Enrolment confirmed." : "Enrolment set back to pending.");
        else Error(result.Error ?? "We couldn't update that enrolment.");
        return RedirectToAction(nameof(Index), new { eventId });
    }
}
