using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Web.Infrastructure;

/// <summary>
/// Protects a controller/action using the session (not cookies/Identity).
/// Unauthenticated users are sent to the login page; users with the wrong role are sent to Access Denied.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    /// <summary>Comma-separated list of roles allowed. Empty = any logged-in user.</summary>
    public string? Roles { get; set; }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        var request = context.HttpContext.Request;

        if (!session.IsLoggedIn())
        {
            var returnUrl = request.Path + request.QueryString;
            // Only remember GET urls - re-posting a form after login makes no sense.
            context.Result = HttpMethods.IsGet(request.Method)
                ? new RedirectToActionResult("Login", "Account", new { area = "", returnUrl })
                : new RedirectToActionResult("Login", "Account", new { area = "" });
            return;
        }

        if (!string.IsNullOrWhiteSpace(Roles))
        {
            var allowed = Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!allowed.Contains(session.Role()))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Home", new { area = "" });
            }
        }
    }
}

/// <summary>Only for anonymous pages (login/register): logged-in users are sent to their home page.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AnonymousOnlyAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        if (session.IsLoggedIn())
            context.Result = new RedirectResult(session.HomeUrl());
    }
}
