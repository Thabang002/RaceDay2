using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.Models.DTOs;
using RaceDay.Models.Entities;
using RaceDay.Services;

namespace RaceDay.Controllers;

[ApiController]
[Route("api/auth")]
[Tags("Authentication")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokenService;

    public AuthController(RaceDayDbContext db, IPasswordHasher hasher, ITokenService tokenService)
    {
        _db = db; _hasher = hasher; _tokenService = tokenService;
    }

    /// <summary>Register a new participant or organiser account.</summary>
    /// <response code="201">User created</response>
    /// <response code="400">Validation error</response>
    /// <response code="409">Email already exists</response>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (dto.Role != "Organiser" && dto.Role != "Participant")
            return BadRequest(new { message = "Role must be 'Organiser' or 'Participant'." });

        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return Conflict(new { message = "Email already exists." });

        var user = new User
        {
            Email = dto.Email,
            FullName = dto.FullName,
            Role = dto.Role,
            PasswordHash = _hasher.Hash(dto.Password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = _tokenService.GenerateToken(user);
        var userDto = new UserDto(user.UserId, user.Email, user.FullName, user.Role, user.CreatedAt);
        return StatusCode(201, new AuthResponseDto(token, userDto));
    }

    /// <summary>Authenticate a user and return a JWT token.</summary>
    /// <response code="200">Login successful</response>
    /// <response code="401">Invalid credentials</response>
    /// <response code="404">User not found</response>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null) return NotFound(new { message = "User doesn't exist." });

        if (!_hasher.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid credentials." });

        var token = _tokenService.GenerateToken(user);
        var userDto = new UserDto(user.UserId, user.Email, user.FullName, user.Role, user.CreatedAt);
        return Ok(new AuthResponseDto(token, userDto));
    }

    /// <summary>Logout (client discards JWT).</summary>
    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout() => Ok(new { message = "Logout successful." });
}