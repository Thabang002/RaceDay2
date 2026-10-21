using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.Models.DTOs;
using RaceDay.Models.Entities;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/locations")]
[Tags("Locations")]
public class LocationsController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    public LocationsController(RaceDayDbContext db) => _db = db;

    /// <summary>Get all locations (public).</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var list = await _db.Locations.Select(l => new LocationDto(
            l.LocationId, l.Name, l.Address, l.City, l.Province, l.PostalCode, l.Latitude, l.Longitude)).ToListAsync();
        return Ok(list);
    }

    /// <summary>Get specific location (public).</summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var l = await _db.Locations.FindAsync(id);
        if (l == null) return NotFound();
        return Ok(new LocationDto(l.LocationId, l.Name, l.Address, l.City, l.Province, l.PostalCode, l.Latitude, l.Longitude));
    }

    /// <summary>Create a new location (Organiser only).</summary>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Create([FromBody] CreateLocationDto dto)
    {
        var l = new Location
        {
            Name = dto.Name,
            Address = dto.Address,
            City = dto.City,
            Province = dto.Province,
            PostalCode = dto.PostalCode,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude
        };
        _db.Locations.Add(l);
        await _db.SaveChangesAsync();
        return StatusCode(201, new LocationDto(l.LocationId, l.Name, l.Address, l.City, l.Province, l.PostalCode, l.Latitude, l.Longitude));
    }
}