using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers;

/// <summary>Profile page (view/update personal details, upload a profile picture). Available to both roles.</summary>
[SessionAuthorize]
public class ProfileController : RaceDayControllerBase
{
    private readonly IRaceDayApiClient _api;
    public ProfileController(IRaceDayApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await _api.GetProfileAsync();
        if (!result.Success || result.Data == null) return LoadFailed(result, HttpContext.Session.HomeUrl());

        HttpContext.Session.UpdateUser(result.Data);
        return View(ToViewModel(result.Data));
    }

    [HttpPost]
    public async Task<IActionResult> Index(ProfileViewModel model)
    {
        if (model.DateOfBirth.HasValue && (model.DateOfBirth.Value.Date > DateTime.Today || model.DateOfBirth.Value.Year < 1900))
            ModelState.AddModelError(nameof(model.DateOfBirth), "Please enter a valid date of birth.");

        if (!ModelState.IsValid) return await RedisplayAsync(model);

        var result = await _api.UpdateProfileAsync(new ProfileRequest(
            model.FullName.Trim(), model.Email.Trim(),
            string.IsNullOrWhiteSpace(model.NewPassword) ? null : model.NewPassword,
            string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim(),
            model.DateOfBirth, model.Gender, string.IsNullOrWhiteSpace(model.City) ? null : model.City.Trim()));

        if (!result.Success || result.Data == null)
        {
            if (result.StatusCode == System.Net.HttpStatusCode.Conflict)
                ModelState.AddModelError(nameof(model.Email), "Another account already uses this email address.");
            else
                AddApiErrors(result);
            return await RedisplayAsync(model);
        }

        HttpContext.Session.UpdateUser(result.Data);
        Success(string.IsNullOrWhiteSpace(model.NewPassword) ? "Profile updated." : "Profile and password updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadPicture(IFormFile? picture)
    {
        var problem = ImageRules.Check(picture);
        if (problem != null)
        {
            Error(problem);
            return RedirectToAction(nameof(Index));
        }

        var result = await _api.UploadProfilePictureAsync(picture!);
        if (result.Success && result.Data != null)
        {
            HttpContext.Session.UpdateUser(result.Data);
            Success("Looking good! Your profile picture has been updated.");
        }
        else
        {
            Error(result.Error ?? "We couldn't upload your picture. Please try again.");
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Re-show the form keeping what the user typed, but refresh display-only fields from the API.</summary>
    private async Task<IActionResult> RedisplayAsync(ProfileViewModel model)
    {
        var current = await _api.GetProfileAsync();
        if (current.Success && current.Data != null)
        {
            model.Role = current.Data.Role;
            model.MemberSince = current.Data.CreatedAt;
            model.ProfilePicture = current.Data.ProfilePicture;
        }
        model.NewPassword = model.ConfirmPassword = null;
        return View(nameof(Index), model);
    }

    private static ProfileViewModel ToViewModel(UserModel u) => new()
    {
        Role = u.Role,
        MemberSince = u.CreatedAt,
        ProfilePicture = u.ProfilePicture,
        FullName = u.FullName,
        Email = u.Email,
        PhoneNumber = u.PhoneNumber,
        DateOfBirth = u.DateOfBirth,
        Gender = u.Gender,
        City = u.City
    };
}
