using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.Models.DTOs;
using RaceDay.Models.Entities;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
[Tags("Enrolments")]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    public EnrolmentsController(RaceDayDbContext db) => _db = db;

    private int CurrentUserId => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    private string CurrentRole => User.FindFirst(System.Security.Claims.ClaimTypes.Role)!.Value;

    /// <summary>Enrol a participant in an event category.</summary>
    [HttpPost("events/{id}/enrol")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> Enrol(int id, [FromBody] CreateEnrolmentDto dto)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null || ev.IsDeleted) return NotFound(new { message = "Event not found." });

        var category = await _db.Categories.FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId && c.EventId == id);
        if (category == null) return NotFound(new { message = "Category not found for this event." });

        if (await _db.Enrolments.AnyAsync(e => e.UserId == CurrentUserId && e.EventId == id && e.Status == "Active"))
            return BadRequest(new { message = "Already enrolled." });

        var currentCount = await _db.Enrolments.CountAsync(e => e.CategoryId == dto.CategoryId && e.Status == "Active");
        if (currentCount >= category.MaxParticipants)
            return BadRequest(new { message = "Category is full." });

        var enrolment = new Enrolment
        {
            UserId = CurrentUserId,
            EventId = id,
            CategoryId = dto.CategoryId,
            PaymentStatus = dto.PaymentStatus ?? "Pending",
            EntryNumber = $"E{id}-{CurrentUserId}-{DateTime.UtcNow.Ticks % 10000}"
        };
        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        return StatusCode(201, new EnrolmentDto(enrolment.EnrolmentId, enrolment.UserId, null,
            enrolment.EventId, ev.Title, enrolment.CategoryId, category.Name,
            enrolment.RegistrationDate, enrolment.Status, enrolment.PaymentStatus, enrolment.EntryNumber));
    }

    /// <summary>Get specific enrolment details.</summary>
    [HttpGet("enrolments/{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var e = await _db.Enrolments
            .Include(x => x.User).Include(x => x.Event).Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.EnrolmentId == id);
        if (e == null) return NotFound();

        if (CurrentRole != "Organiser" && e.UserId != CurrentUserId) return Forbid();

        return Ok(new EnrolmentDto(e.EnrolmentId, e.UserId, e.User?.FullName,
            e.EventId, e.Event?.Title, e.CategoryId, e.Category?.Name,
            e.RegistrationDate, e.Status, e.PaymentStatus, e.EntryNumber));
    }

    /// <summary>Update enrolment status (Participant self only).</summary>
    [HttpPut("enrolments/{id}")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEnrolmentDto dto)
    {
        var e = await _db.Enrolments.FindAsync(id);
        if (e == null) return NotFound();
        if (e.UserId != CurrentUserId) return Forbid();

        e.Status = dto.Status;
        await _db.SaveChangesAsync();
        return Ok(new { e.EnrolmentId, e.Status });
    }

    /// <summary>Cancel/withdraw an enrolment.</summary>
    [HttpDelete("enrolments/{id}")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> Cancel(int id)
    {
        var e = await _db.Enrolments.FindAsync(id);
        if (e == null) return NotFound();
        if (e.UserId != CurrentUserId) return Forbid();

        _db.Enrolments.Remove(e);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Update payment status (Organiser only).</summary>
    [HttpPut("enrolments/{id}/payment")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> UpdatePayment(int id, [FromBody] UpdatePaymentDto dto)
    {
        var e = await _db.Enrolments.Include(x => x.Event).FirstOrDefaultAsync(x => x.EnrolmentId == id);
        if (e == null) return NotFound();
        if (e.Event!.CreatedBy != CurrentUserId) return Forbid();

        e.PaymentStatus = dto.PaymentStatus;
        await _db.SaveChangesAsync();
        return Ok(new { e.EnrolmentId, e.PaymentStatus });
    }
}