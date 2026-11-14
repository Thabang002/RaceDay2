using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Areas.Organiser.Controllers;

public class LocationsController : OrganiserControllerBase
{
    public LocationsController(IRaceDayApiClient api) : base(api) { }

    [HttpGet]
    public IActionResult Create(string? returnUrl = null) => View(new LocationFormViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    public async Task<IActionResult> Create(LocationFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await Api.CreateLocationAsync(new LocationRequest(
            model.Name.Trim(), model.Address?.Trim(), model.City?.Trim(), model.Province,
            model.PostalCode?.Trim(), null, null));

        if (!result.Success || result.Data == null)
        {
            AddApiErrors(result);
            return View(model);
        }

        Success($"Venue \"{result.Data.Name}\" added.");

        // Go back to the event form with the new venue pre-selected.
        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            var sep = model.ReturnUrl.Contains('?') ? "&" : "?";
            return LocalRedirect($"{model.ReturnUrl}{sep}locationId={result.Data.LocationId}");
        }
        return RedirectToAction("Create", "Events", new { locationId = result.Data.LocationId });
    }
}
