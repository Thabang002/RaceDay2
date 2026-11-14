using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers;

public class AccountController : RaceDayControllerBase
{
    private readonly IRaceDayApiClient _api;
    public AccountController(IRaceDayApiClient api) => _api = api;

    [HttpGet, AnonymousOnly]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost, AnonymousOnly]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _api.LoginAsync(new LoginRequest(model.Email.Trim(), model.Password));
        if (!result.Success || result.Data == null)
        {
            // Don't reveal whether the email exists - same message for 401 and 404.
            var message = result.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.NotFound
                ? "Invalid email or password."
                : result.Error ?? "Login failed. Please try again.";
            ModelState.AddModelError(string.Empty, message);
            model.Password = string.Empty;
            return View(model);
        }

        HttpContext.Session.SignIn(result.Data.Token, result.Data.User);
        Success($"Welcome back, {FirstName(result.Data.User.FullName)}!");

        // Only follow local return URLs that point into this user's own area (or the shared profile page).
        var returnUrl = model.ReturnUrl;
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) &&
            (returnUrl.StartsWith("/" + result.Data.User.Role + "/", StringComparison.OrdinalIgnoreCase) ||
             returnUrl.StartsWith("/Profile", StringComparison.OrdinalIgnoreCase)))
        {
            return LocalRedirect(returnUrl);
        }

        return Redirect(HttpContext.Session.HomeUrl());
    }

    [HttpGet, AnonymousOnly]
    public IActionResult Register(string? role = null) =>
        View(new RegisterViewModel { Role = role == Roles.Organiser ? Roles.Organiser : Roles.Participant });

    [HttpPost, AnonymousOnly]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _api.RegisterAsync(new RegisterRequest(
            model.Email.Trim(), model.Password, model.FullName.Trim(), model.Role));

        if (!result.Success || result.Data == null)
        {
            if (result.StatusCode == System.Net.HttpStatusCode.Conflict)
                ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
            else
                AddApiErrors(result);
            model.Password = model.ConfirmPassword = string.Empty;
            return View(model);
        }

        HttpContext.Session.SignIn(result.Data.Token, result.Data.User);
        Success(model.Role == Roles.Organiser
            ? "Your organiser account is ready. Let's create your first event!"
            : $"Welcome to RaceDay, {FirstName(model.FullName)}! Find your next race below.");
        return Redirect(HttpContext.Session.HomeUrl());
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        if (HttpContext.Session.IsLoggedIn()) await _api.LogoutAsync();
        HttpContext.Session.Clear();
        Success("You have been logged out. See you at the start line!");
        return RedirectToAction(nameof(Login));
    }

    private static string FirstName(string fullName) => fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? fullName;
}
