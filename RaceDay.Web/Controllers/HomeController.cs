using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : RaceDayControllerBase
{
    private readonly IRaceDayApiClient _api;
    public HomeController(IRaceDayApiClient api) => _api = api;

    /// <summary>Public landing page. Logged-in users go straight to their role's home page.</summary>
    public async Task<IActionResult> Index()
    {
        if (HttpContext.Session.IsLoggedIn()) return Redirect(HttpContext.Session.HomeUrl());

        var events = await _api.GetEventsAsync(new EventFilter { Upcoming = true });
        ViewBag.ApiDown = !events.Success;
        return View(events.Data?.Take(6).ToList() ?? new List<EventModel>());
    }

    public IActionResult AccessDenied() => View();

    // Error pages can be re-executed from a failed POST, so they must not demand an anti-forgery token.
    [Route("Home/StatusCode")]
    [IgnoreAntiforgeryToken]
    public IActionResult StatusCodePage(int code)
    {
        Response.StatusCode = code;
        ViewBag.Code = code;
        return View("StatusCode");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

public class ErrorViewModel
{
    public string? RequestId { get; set; }
}
