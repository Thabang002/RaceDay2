using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.Models.DTOs;
using RaceDay.Services;

namespace RaceDay.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
[Tags("User Profile")]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IBlobStorageService _blobs;

    public UsersController(RaceDayDbContext db, IPasswordHasher hasher, IBlobStorageService blobs)
    {
        _db = db; _hasher = hasher; _blobs = blobs;
    }

    private int CurrentUserId => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    private string CurrentRole => User.FindFirst(System.Security.Claims.ClaimTypes.Role)!.Value;

    /// <summary>Get current logged-in user's profile.</summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var user = await _db.Users.FindAsync(CurrentUserId);
        if (user == null) return NotFound(new { message = "User not found." });
        return Ok(user.ToDto());
    }

    /// <summary>Update current user's profile.</summary>
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var user = await _db.Users.FindAsync(CurrentUserId);
        if (user == null) return NotFound(new { message = "User not found." });

        if (user.Email != dto.Email && await _db.Users.AnyAsync(u => u.Email == dto.Email && u.UserId != CurrentUserId))
            return Conflict(new { message = "Email already exists." });

        if (dto.DateOfBirth.HasValue &&
            (dto.DateOfBirth.Value.Date > DateTime.UtcNow.Date || dto.DateOfBirth.Value.Year < 1900))
            return BadRequest(new { message = "Please enter a valid date of birth." });

        user.FullName = dto.FullName;
        user.Email = dto.Email;
        user.PhoneNumber = dto.PhoneNumber;
        user.DateOfBirth = dto.DateOfBirth?.Date;
        user.Gender = dto.Gender;
        user.City = dto.City;
        if (!string.IsNullOrWhiteSpace(dto.Password))
            user.PasswordHash = _hasher.Hash(dto.Password);

        await _db.SaveChangesAsync();
        return Ok(user.ToDto());
    }

    /// <summary>Upload or replace the current user's profile picture (stored in Azure Blob Storage).</summary>
    [HttpPost("profile/picture")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file, CancellationToken ct)
    {
        var user = await _db.Users.FindAsync([CurrentUserId], ct);
        if (user == null) return NotFound(new { message = "User not found." });

        string blobName;
        try
        {
            blobName = await _blobs.UploadImageAsync(file, "profiles", ct);
        }
        catch (ImageValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        var old = user.ProfilePicture;
        user.ProfilePicture = blobName;
        await _db.SaveChangesAsync(ct);
        await _blobs.DeleteAsync(old, ct);

        return Ok(user.ToDto());
    }

    /// <summary>Get enrolments for a user (Organiser or Self).</summary>
    [HttpGet("{id:int}/enrolments")]
    public async Task<IActionResult> GetUserEnrolments(int id)
    {
        if (CurrentRole != "Organiser" && CurrentUserId != id)
            return Forbid();

        var userExists = await _db.Users.AnyAsync(u => u.UserId == id);
        if (!userExists) return NotFound(new { message = "User not found." });

        var rows = await _db.Enrolments
            .Where(e => e.UserId == id && !e.Event!.IsDeleted)
            .OrderBy(e => e.Event!.Date)
            .Select(e => new
            {
                e.EnrolmentId, e.UserId, UserName = e.User!.FullName, UserEmail = e.User.Email,
                e.EventId, EventTitle = e.Event!.Title, EventDate = e.Event.Date,
                e.CategoryId, CategoryName = e.Category!.Name,
                e.RegistrationDate, e.Status, e.PaymentStatus, e.EntryNumber
            })
            .ToListAsync();

        var enrolments = rows.Select(e => new EnrolmentDto(e.EnrolmentId, e.UserId, e.UserName,
            e.EventId, e.EventTitle, e.CategoryId, e.CategoryName,
            e.RegistrationDate, e.Status, e.PaymentStatus, e.EntryNumber,
            e.EventDate, EnrolmentStatus.Display(e.Status, e.PaymentStatus), e.UserEmail));

        return Ok(enrolments);
    }
}
