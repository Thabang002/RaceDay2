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

    public UsersController(RaceDayDbContext db, IPasswordHasher hasher)
    {
        _db = db; _hasher = hasher;
    }

    private int CurrentUserId => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    private string CurrentRole => User.FindFirst(System.Security.Claims.ClaimTypes.Role)!.Value;

    /// <summary>Get current logged-in user's profile.</summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var user = await _db.Users.FindAsync(CurrentUserId);
        if (user == null) return NotFound();
        return Ok(new UserDto(user.UserId, user.Email, user.FullName, user.Role, user.CreatedAt));
    }

    /// <summary>Update current user's profile.</summary>
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var user = await _db.Users.FindAsync(CurrentUserId);
        if (user == null) return NotFound();

        if (user.Email != dto.Email && await _db.Users.AnyAsync(u => u.Email == dto.Email && u.UserId != CurrentUserId))
            return Conflict(new { message = "Email already exists." });

        user.FullName = dto.FullName;
        user.Email = dto.Email;
        if (!string.IsNullOrWhiteSpace(dto.Password))
            user.PasswordHash = _hasher.Hash(dto.Password);

        await _db.SaveChangesAsync();
        return Ok(new UserDto(user.UserId, user.Email, user.FullName, user.Role, user.CreatedAt));
    }

    /// <summary>Get enrolments for a user (Organiser or Self).</summary>
    [HttpGet("{id}/enrolments")]
    public async Task<IActionResult> GetUserEnrolments(int id)
    {
        if (CurrentRole != "Organiser" && CurrentUserId != id)
            return Forbid();

        var userExists = await _db.Users.AnyAsync(u => u.UserId == id);
        if (!userExists) return NotFound();

        var enrolments = await _db.Enrolments
            .Include(e => e.Event).Include(e => e.Category).Include(e => e.User)
            .Where(e => e.UserId == id)
            .Select(e => new EnrolmentDto(e.EnrolmentId, e.UserId, e.User!.FullName,
                e.EventId, e.Event!.Title, e.CategoryId, e.Category!.Name,
                e.RegistrationDate, e.Status, e.PaymentStatus, e.EntryNumber))
            .ToListAsync();

        return Ok(enrolments);
    }
}