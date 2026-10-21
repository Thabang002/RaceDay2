using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.Models.DTOs;
using RaceDay.Models.Entities;

namespace RaceDay.Controllers;

[ApiController]
[Route("api")]
[Tags("Categories")]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    public CategoriesController(RaceDayDbContext db) => _db = db;

    private int CurrentUserId => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

    /// <summary>Get specific category details (public).</summary>
    [HttpGet("categories/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var c = await _db.Categories.FindAsync(id);
        if (c == null) return NotFound();
        return Ok(new CategoryDto(c.CategoryId, c.EventId, c.Name, c.Description, c.DistanceKm, c.StartFee, c.MaxParticipants));
    }

    /// <summary>Create category for an event (owner Organiser only).</summary>
    [HttpPost("events/{id}/categories")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Create(int id, [FromBody] CreateCategoryDto dto)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound();
        if (ev.CreatedBy != CurrentUserId) return Forbid();

        var c = new Category
        {
            EventId = id,
            Name = dto.Name,
            Description = dto.Description,
            DistanceKm = dto.DistanceKm,
            StartFee = dto.StartFee,
            MaxParticipants = dto.MaxParticipants
        };
        _db.Categories.Add(c);
        await _db.SaveChangesAsync();
        return StatusCode(201, new CategoryDto(c.CategoryId, c.EventId, c.Name, c.Description, c.DistanceKm, c.StartFee, c.MaxParticipants));
    }

    /// <summary>Update a category (owner Organiser only).</summary>
    [HttpPut("categories/{id}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryDto dto)
    {
        // Update implementation
    }
}       