using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.Models.DTOs;
using RaceDay.Models.Entities;

namespace RaceDay.Controllers;

[ApiController]
[Route("api/events")]
[Tags("Events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    public EventsController(RaceDayDbContext db) => _db = db;

    private int CurrentUserId => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    private string CurrentRole => User.FindFirst(System.Security.Claims.ClaimTypes.Role)!.Value;

    /// <summary>Get all events (public) with optional filters.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] DateTime? date, [FromQuery] int? locationId, [FromQuery] string? status)
    {
        var query = _db.Events.Include(e => e.Location).Include(e => e.Organiser)
            .Where(e => !e.IsDeleted).AsQueryable();

        if (date.HasValue) query = query.Where(e => e.Date.Date == date.Value.Date);
        if (locationId.HasValue) query = query.Where(e => e.LocationId == locationId);
        if (!string.IsNullOrEmpty(status)) query = query.Where(e => e.Status == status);

        var events = await query.Select(e => new EventDto(
            e.EventId, e.Title, e.Description, e.Date, e.StartTime,
            e.LocationId, e.Location!.Name, e.MaxParticipants, e.Status,
            e.CreatedBy, e.Organiser!.FullName, e.CreatedAt)).ToListAsync();

        return Ok(events);
    }

    /// <summary>Get specific event details (public).</summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var e = await _db.Events.Include(x => x.Location).Include(x => x.Organiser)
            .FirstOrDefaultAsync(x => x.EventId == id && !x.IsDeleted);
        if (e == null) return NotFound();

        return Ok(new EventDto(e.EventId, e.Title, e.Description, e.Date, e.StartTime,
            e.LocationId, e.Location?.Name, e.MaxParticipants, e.Status,
            e.CreatedBy, e.Organiser?.FullName, e.CreatedAt));
    }

    /// <summary>Create a new event (Organiser only).</summary>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Create([FromBody] CreateEventDto dto)
    {
        if (!await _db.Locations.AnyAsync(l => l.LocationId == dto.LocationId))
            return BadRequest(new { message = "Invalid LocationId." });

        var ev = new Event
        {
            Title = dto.Title,
            Description = dto.Description,
            Date = dto.Date,
            StartTime = dto.StartTime,
            LocationId = dto.LocationId,
            MaxParticipants = dto.MaxParticipants,
            CreatedBy = CurrentUserId
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        // Auto-add organiser to EventOrganisers
        _db.EventOrganisers.Add(new EventOrganiser { EventId = ev.EventId, UserId = CurrentUserId, Role = "Owner" });
        await _db.SaveChangesAsync();

        return StatusCode(201, new { ev.EventId, ev.Title, ev.Date, ev.Status });
    }

    /// <summary>Update an event (owner Organiser only).</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEventDto dto)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null || ev.IsDeleted) return NotFound();
        if (ev.CreatedBy != CurrentUserId) return Forbid();

        ev.Title = dto.Title;
        ev.Description = dto.Description;
        ev.Date = dto.Date;
        ev.StartTime = dto.StartTime;
        ev.LocationId = dto.LocationId;
        ev.MaxParticipants = dto.MaxParticipants;
        if (!string.IsNullOrEmpty(dto.Status)) ev.Status = dto.Status;

        await _db.SaveChangesAsync();
        return Ok(ev);
    }

    /// <summary>Soft-delete an event (owner Organiser only).</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null || ev.IsDeleted) return NotFound();
        if (ev.CreatedBy != CurrentUserId) return Forbid();

        ev.IsDeleted = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Get all categories for an event (public).</summary>
    [HttpGet("{id}/categories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategories(int id)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == id)) return NotFound();

        var cats = await _db.Categories.Where(c => c.EventId == id)
            .Select(c => new CategoryDto(c.CategoryId, c.EventId, c.Name, c.Description,
                c.DistanceKm, c.StartFee, c.MaxParticipants)).ToListAsync();
        return Ok(cats);
    }

    /// <summary>Get all enrolments for an event (owner Organiser only).</summary>
    [HttpGet("{id}/enrolments")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> GetEnrolments(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound();
        if (ev.CreatedBy != CurrentUserId) return Forbid();

        var list = await _db.Enrolments
            .Include(e => e.User).Include(e => e.Category).Include(e => e.Event)
            .Where(e => e.EventId == id)
            .Select(e => new EnrolmentDto(e.EnrolmentId, e.UserId, e.User!.FullName,
                e.EventId, e.Event!.Title, e.CategoryId, e.Category!.Name,
                e.RegistrationDate, e.Status, e.PaymentStatus, e.EntryNumber)).ToListAsync();
        return Ok(list);
    }

    /// <summary>Get weather forecast for an event (public).</summary>
    [HttpGet("{id}/weather")]
    [AllowAnonymous]
    public async Task<IActionResult> GetWeather(int id)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == id)) return NotFound();
        var weather = await _db.WeatherData.Where(w => w.EventId == id).ToListAsync();
        if (!weather.Any()) return NotFound(new { message = "No weather data." });
        return Ok(weather);
    }
}