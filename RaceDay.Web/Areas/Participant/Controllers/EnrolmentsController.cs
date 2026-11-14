using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Areas.Participant.Controllers;

public class EnrolmentsController : ParticipantControllerBase
{
    public EnrolmentsController(IRaceDayApiClient api) : base(api) { }

    /// <summary>All events the participant has enrolled for, with their status.</summary>
    public async Task<IActionResult> Index()
    {
        var result = await Api.GetUserEnrolmentsAsync(CurrentUserId);
        if (!result.Success) Error(result.Error ?? "We couldn't load your enrolments.");

        var all = result.Data ?? new();
        return View(new MyEnrolmentsViewModel
        {
            Upcoming = all.Where(e => e.IsUpcoming).OrderBy(e => e.EventDate).ToList(),
            Past = all.Where(e => !e.IsUpcoming).OrderByDescending(e => e.EventDate).ToList()
        });
    }

    [HttpPost]
    public async Task<IActionResult> Withdraw(int id)
    {
        var result = await Api.WithdrawAsync(id);
        if (result.Success) Success("You have withdrawn from the event.");
        else Error(result.Error ?? "We couldn't withdraw your enrolment.");
        return RedirectToAction(nameof(Index));
    }
}
