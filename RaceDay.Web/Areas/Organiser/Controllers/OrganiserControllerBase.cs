using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Areas.Organiser.Controllers;

[Area("Organiser")]
[SessionAuthorize(Roles = Roles.Organiser)]
public abstract class OrganiserControllerBase : RaceDayControllerBase
{
    protected readonly IRaceDayApiClient Api;
    protected OrganiserControllerBase(IRaceDayApiClient api) => Api = api;

    /// <summary>Loads an event and makes sure it belongs to the logged-in organiser.</summary>
    protected async Task<(EventModel? Event, IActionResult? Failure)> LoadOwnEventAsync(int eventId)
    {
        var result = await Api.GetEventAsync(eventId);
        if (!result.Success || result.Data == null)
            return (null, LoadFailed(result, "/Organiser/Events"));

        if (result.Data.CreatedBy != CurrentUserId)
            return (null, RedirectToAction("AccessDenied", "Home", new { area = "" }));

        return (result.Data, null);
    }
}
