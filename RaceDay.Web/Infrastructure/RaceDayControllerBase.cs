using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Infrastructure;

/// <summary>Shared helpers for showing API outcomes to the user in a friendly way.</summary>
public abstract class RaceDayControllerBase : Controller
{
    protected int CurrentUserId => HttpContext.Session.UserId();

    protected void Success(string message) => TempData["Success"] = message;
    protected void Error(string message) => TempData["Error"] = message;
    protected void Warning(string message) => TempData["Warning"] = message;

    /// <summary>Copies API validation errors onto the form so they appear next to the right fields.</summary>
    protected void AddApiErrors(ApiResult result, string prefix = "")
    {
        foreach (var (field, messages) in result.FieldErrors)
        {
            var key = string.IsNullOrEmpty(prefix) ? field : $"{prefix}.{field}";
            foreach (var m in messages) ModelState.AddModelError(key, m);
        }
        if (!string.IsNullOrWhiteSpace(result.Error))
            ModelState.AddModelError(string.Empty, result.Error);
    }

    /// <summary>Standard response when loading a page's main record fails.</summary>
    protected IActionResult LoadFailed(ApiResult result, string fallbackUrl)
    {
        if (result.IsNotFound) return NotFound();
        if (result.IsForbidden) return RedirectToAction("AccessDenied", "Home", new { area = "" });
        Error(result.Error ?? "Something went wrong. Please try again.");
        return LocalRedirect(fallbackUrl);
    }
}
