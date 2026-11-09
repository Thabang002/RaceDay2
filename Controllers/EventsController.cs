using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.Models.DTOs;
using RaceDay.Models.Entities;
using RaceDay.Services;

namespace RaceDay.Controllers;

[ApiController]
[Route("api/events")]
[Tags("Events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IBlobStorageService _blobs;

    public EventsController(RaceDayDbContext db, IBlobStorageService blobs)
    {
        _db = db;
        _blobs = blobs;
    }

    private int CurrentUserId => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

    /// <summary>Get all events (public) with optional filters.</summary>
    /// <param name="date">Exact event date.</param>
    /// <param name="locationId">Location filter.</param>
    /// <param name="status">Event status filter.</param>
    /// <param name="search">Matches title, description or location name.</param>
    /// <param name="eventType">e.g. "Marathon", "Trail Run".</param>
    /// <param name="from">Events on or after this date.</param>
    /// <param name="to">Events on or before this date.</param>
    /// <param name="upcoming">Only events from today onwards that are not cancelled/completed.</param>
    /// <param name="minDistance">Minimum distance in km.</param>
    /// <param name="maxDistance">Maximum distance in km.</param>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTime? date, [FromQuery] int? locationId, [FromQuery] string? status,
        [FromQuery] string? search, [FromQuery] string? eventType,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] bool upcoming = false,
        [FromQuery] decimal? minDistance = null, [FromQuery] decimal? maxDistance = null)
    {
        var query = _db.Events.Include(e => e.Location).Include(e => e.Organiser)
            .Where(e => !e.IsDeleted).AsQueryable();

        if (date.HasValue) query = query.Where(e => e.Date.Date == date.Value.Date);
        if (locationId.HasValue) query = query.Where(e => e.LocationId == locationId);
        if (!string.IsNullOrEmpty(status)) query = query.Where(e => e.Status == status);
        if (!string.IsNullOrWhiteSpace(eventType)) query = query.Where(e => e.EventType == eventType);
        if (from.HasValue) query = query.Where(e => e.Date >= from.Value.Date);
        if (to.HasValue) query = query.Where(e => e.Date <= to.Value.Date);
        if (minDistance.HasValue) query = query.Where(e => e.DistanceKm >= minDistance);
        if (maxDistance.HasValue) query = query.Where(e => e.DistanceKm <= maxDistance);
        if (upcoming)
        {
            var today = DateTime.UtcNow.Date;
            query = query.Where(e => e.Date >= today && e.Status != "Cancelled" && e.Status != "Completed");
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e => e.Title.Contains(term)
                || (e.Description != null && e.Description.Contains(term))
                || (e.Location != null && (e.Location.Name.Contains(term) || (e.Location.City != null && e.Location.City.Contains(term)))));
        }

        var events = await query.OrderBy(e => e.Date).ThenBy(e => e.StartTime)
            .Select(e => new EventDto(
                e.EventId, e.Title, e.Description, e.Date, e.StartTime,
                e.LocationId, e.Location!.Name, e.MaxParticipants, e.Status,
                e.CreatedBy, e.Organiser!.FullName, e.CreatedAt,
                e.DistanceKm, e.EventType, e.BannerImage, e.ResultsPublished, e.Location.City))
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>List of supported event types (public).</summary>
    [HttpGet("types")]
    [AllowAnonymous]
    public IActionResult GetTypes() => Ok(EventTypes.All);

    /// <summary>Events created by the logged-in organiser, with enrolment totals (dashboard).</summary>
    [HttpGet("mine")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> GetMine()
    {
        var uid = CurrentUserId;
        var list = await _db.Events
            .Where(e => e.CreatedBy == uid && !e.IsDeleted)
            .OrderBy(e => e.Date)
            .Select(e => new OrganiserEventSummaryDto(
                e.EventId, e.Title, e.Date, e.StartTime,
                e.Location!.Name, e.DistanceKm, e.EventType,
                e.Status, e.MaxParticipants,
                e.Enrolments.Count(en => en.Status == EnrolmentStatus.Active),
                e.Enrolments.Count(en => en.Status == EnrolmentStatus.Active
                    && (en.PaymentStatus == "Paid" || en.PaymentStatus == "Confirmed")),
                e.Categories.Count,
                e.ResultsPublished, e.BannerImage))
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>Get specific event details (public).</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var e = await _db.Events.Include(x => x.Location).Include(x => x.Organiser)
            .FirstOrDefaultAsync(x => x.EventId == id && !x.IsDeleted);
        if (e == null) return NotFound(new { message = "Event not found." });

        return Ok(ToDto(e));
    }

    /// <summary>Create a new event (Organiser only).</summary>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Create([FromBody] CreateEventDto dto)
    {
        if (!await _db.Locations.AnyAsync(l => l.LocationId == dto.LocationId))
            return BadRequest(new { message = "Invalid LocationId." });
        if (dto.EventType != null && !EventTypes.All.Contains(dto.EventType))
            return BadRequest(new { message = "Invalid event type." });

        var ev = new Event
        {
            Title = dto.Title,
            Description = dto.Description,
            Date = dto.Date,
            StartTime = dto.StartTime,
            LocationId = dto.LocationId,
            MaxParticipants = dto.MaxParticipants,
            DistanceKm = dto.DistanceKm,
            EventType = dto.EventType,
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
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEventDto dto)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null || ev.IsDeleted) return NotFound(new { message = "Event not found." });
        if (ev.CreatedBy != CurrentUserId) return Forbid();

        if (!await _db.Locations.AnyAsync(l => l.LocationId == dto.LocationId))
            return BadRequest(new { message = "Invalid LocationId." });
        if (dto.EventType != null && !EventTypes.All.Contains(dto.EventType))
            return BadRequest(new { message = "Invalid event type." });
        if (!string.IsNullOrEmpty(dto.Status) && !EventStatuses.All.Contains(dto.Status))
            return BadRequest(new { message = "Invalid status." });

        ev.Title = dto.Title;
        ev.Description = dto.Description;
        ev.Date = dto.Date;
        ev.StartTime = dto.StartTime;
        ev.LocationId = dto.LocationId;
        ev.MaxParticipants = dto.MaxParticipants;
        ev.DistanceKm = dto.DistanceKm;
        ev.EventType = dto.EventType;
        if (!string.IsNullOrEmpty(dto.Status)) ev.Status = dto.Status;

        await _db.SaveChangesAsync();

        await _db.Entry(ev).Reference(x => x.Location).LoadAsync();
        await _db.Entry(ev).Reference(x => x.Organiser).LoadAsync();
        return Ok(ToDto(ev));
    }

    /// <summary>Upload or replace the event banner image (owner Organiser only). Stored in Azure Blob Storage.</summary>
    [HttpPost("{id:int}/banner")]
    [Authorize(Roles = "Organiser")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadBanner(int id, IFormFile file, CancellationToken ct)
    {
        var ev = await _db.Events.FindAsync([id], ct);
        if (ev == null || ev.IsDeleted) return NotFound(new { message = "Event not found." });
        if (ev.CreatedBy != CurrentUserId) return Forbid();

        string blobName;
        try
        {
            blobName = await _blobs.UploadImageAsync(file, "banners", ct);
        }
        catch (ImageValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        var old = ev.BannerImage;
        ev.BannerImage = blobName;
        await _db.SaveChangesAsync(ct);
        await _blobs.DeleteAsync(old, ct);

        return Ok(new { ev.EventId, bannerImage = blobName });
    }

    /// <summary>Soft-delete an event (owner Organiser only).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null || ev.IsDeleted) return NotFound(new { message = "Event not found." });
        if (ev.CreatedBy != CurrentUserId) return Forbid();

        ev.IsDeleted = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Get all categories for an event (public), including how many places are taken.</summary>
    [HttpGet("{id:int}/categories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategories(int id)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == id && !e.IsDeleted)) return NotFound(new { message = "Event not found." });

        var cats = await _db.Categories.Where(c => c.EventId == id)
            .OrderBy(c => c.DistanceKm).ThenBy(c => c.Name)
            .Select(c => new CategoryDto(c.CategoryId, c.EventId, c.Name, c.Description,
                c.DistanceKm, c.StartFee, c.MaxParticipants,
                c.Enrolments.Count(en => en.Status == EnrolmentStatus.Active)))
            .ToListAsync();
        return Ok(cats);
    }

    /// <summary>Get all enrolments for an event (owner Organiser only).</summary>
    [HttpGet("{id:int}/enrolments")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> GetEnrolments(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null || ev.IsDeleted) return NotFound(new { message = "Event not found." });
        if (ev.CreatedBy != CurrentUserId) return Forbid();

        var rows = await _db.Enrolments
            .Where(e => e.EventId == id)
            .OrderBy(e => e.Category!.Name).ThenBy(e => e.User!.FullName)
            .Select(e => new
            {
                e.EnrolmentId, e.UserId, UserName = e.User!.FullName, UserEmail = e.User.Email,
                e.EventId, EventTitle = e.Event!.Title, EventDate = e.Event.Date,
                e.CategoryId, CategoryName = e.Category!.Name,
                e.RegistrationDate, e.Status, e.PaymentStatus, e.EntryNumber
            })
            .ToListAsync();

        var list = rows.Select(e => new EnrolmentDto(e.EnrolmentId, e.UserId, e.UserName,
            e.EventId, e.EventTitle, e.CategoryId, e.CategoryName,
            e.RegistrationDate, e.Status, e.PaymentStatus, e.EntryNumber,
            e.EventDate, EnrolmentStatus.Display(e.Status, e.PaymentStatus), e.UserEmail));

        return Ok(list);
    }

    /// <summary>Get weather forecast for an event (public).</summary>
    [HttpGet("{id:int}/weather")]
    [AllowAnonymous]
    public async Task<IActionResult> GetWeather(int id)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == id)) return NotFound();
        var weather = await _db.WeatherData.Where(w => w.EventId == id)
            .Select(w => new { w.WeatherId, w.EventId, w.ForecastDate, w.Temperature, w.Conditions, w.WindSpeed, w.Humidity, w.LastUpdated })
            .ToListAsync();
        if (!weather.Any()) return NotFound(new { message = "No weather data." });
        return Ok(weather);
    }

    private static EventDto ToDto(Event e) => new(
        e.EventId, e.Title, e.Description, e.Date, e.StartTime,
        e.LocationId, e.Location?.Name, e.MaxParticipants, e.Status,
        e.CreatedBy, e.Organiser?.FullName, e.CreatedAt,
        e.DistanceKm, e.EventType, e.BannerImage, e.ResultsPublished, e.Location?.City);
}
