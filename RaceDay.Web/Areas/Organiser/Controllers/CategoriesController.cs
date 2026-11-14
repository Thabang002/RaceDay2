using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Areas.Organiser.Controllers;

public class CategoriesController : OrganiserControllerBase
{
    public CategoriesController(IRaceDayApiClient api) : base(api) { }

    /// <summary>Categories for one event, with an "add category" form on the same page.</summary>
    public async Task<IActionResult> Index(int eventId)
    {
        var (ev, failure) = await LoadOwnEventAsync(eventId);
        if (failure != null) return failure;

        return View(await BuildPageAsync(ev!, new CategoryFormViewModel
        {
            EventId = eventId,
            DistanceKm = ev!.DistanceKm,
            StartFee = 0,
            MaxParticipants = ev.MaxParticipants
        }));
    }

    [HttpPost]
    public async Task<IActionResult> Create([Bind(Prefix = "NewCategory")] CategoryFormViewModel model)
    {
        var (ev, failure) = await LoadOwnEventAsync(model.EventId);
        if (failure != null) return failure;

        if (ModelState.IsValid)
        {
            var result = await Api.CreateCategoryAsync(model.EventId, ToRequest(model));
            if (result.Success)
            {
                Success($"Category \"{model.Name}\" added.");
                return RedirectToAction(nameof(Index), new { eventId = model.EventId });
            }
            AddApiErrors(result, "NewCategory");
        }

        return View(nameof(Index), await BuildPageAsync(ev!, model));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await Api.GetCategoryAsync(id);
        if (!result.Success || result.Data == null) return LoadFailed(result, "/Organiser/Events");

        var (ev, failure) = await LoadOwnEventAsync(result.Data.EventId);
        if (failure != null) return failure;

        ViewBag.Event = ev;
        ViewBag.EnrolledCount = result.Data.EnrolledCount;
        return View(new CategoryFormViewModel
        {
            CategoryId = result.Data.CategoryId,
            EventId = result.Data.EventId,
            Name = result.Data.Name,
            Description = result.Data.Description,
            DistanceKm = result.Data.DistanceKm,
            StartFee = result.Data.StartFee,
            MaxParticipants = result.Data.MaxParticipants
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, CategoryFormViewModel model)
    {
        model.CategoryId = id;
        var (ev, failure) = await LoadOwnEventAsync(model.EventId);
        if (failure != null) return failure;

        if (ModelState.IsValid)
        {
            var result = await Api.UpdateCategoryAsync(id, ToRequest(model));
            if (result.Success)
            {
                Success("Category updated.");
                return RedirectToAction(nameof(Index), new { eventId = model.EventId });
            }
            if (result.IsForbidden) return RedirectToAction("AccessDenied", "Home", new { area = "" });
            AddApiErrors(result);
        }

        ViewBag.Event = ev;
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, int eventId)
    {
        var result = await Api.DeleteCategoryAsync(id);
        if (result.Success) Success("Category removed.");
        else Error(result.Error ?? "We couldn't remove that category.");
        return RedirectToAction(nameof(Index), new { eventId });
    }

    private async Task<CategoriesPageViewModel> BuildPageAsync(EventModel ev, CategoryFormViewModel form)
    {
        var categories = await Api.GetCategoriesAsync(ev.EventId);
        if (!categories.Success) ModelState.AddModelError(string.Empty, categories.Error ?? "Categories could not be loaded.");
        return new CategoriesPageViewModel { Event = ev, Categories = categories.Data ?? new(), NewCategory = form };
    }

    private static CategoryRequest ToRequest(CategoryFormViewModel m) => new(
        m.Name.Trim(), m.Description?.Trim(), m.DistanceKm ?? 0, m.StartFee ?? 0, m.MaxParticipants ?? 1);
}
