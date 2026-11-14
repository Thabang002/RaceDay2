using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Areas.Organiser.Controllers;

public class DashboardController : OrganiserControllerBase
{
    public DashboardController(IRaceDayApiClient api) : base(api) { }

    public async Task<IActionResult> Index()
    {
        var result = await Api.GetMyEventsAsync();
        if (!result.Success) Error(result.Error ?? "We couldn't load your events.");

        return View(new OrganiserDashboardViewModel
        {
            OrganiserName = HttpContext.Session.FullName(),
            Events = result.Data ?? new()
        });
    }
}
