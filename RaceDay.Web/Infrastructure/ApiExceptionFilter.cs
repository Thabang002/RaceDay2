using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using RaceDay.Web.Services;

namespace RaceDay.Web.Infrastructure;

/// <summary>
/// Turns an expired/invalid API token into a clean logout + redirect to login,
/// instead of an error page.
/// </summary>
public sealed class ApiExceptionFilter : IExceptionFilter
{
    private readonly ITempDataDictionaryFactory _tempData;
    private readonly ILogger<ApiExceptionFilter> _logger;

    public ApiExceptionFilter(ITempDataDictionaryFactory tempData, ILogger<ApiExceptionFilter> logger)
    {
        _tempData = tempData;
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not ApiUnauthorizedException) return;

        _logger.LogInformation("API rejected the session token - signing the user out.");
        context.HttpContext.Session.Clear();
        _tempData.GetTempData(context.HttpContext)["Error"] = "Your session has expired. Please log in again.";
        context.Result = new RedirectToActionResult("Login", "Account", new { area = "" });
        context.ExceptionHandled = true;
    }
}
