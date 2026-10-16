using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data.API.Data;
using RaceDay.DTOs.API.DTOs;
using RaceDay.Models.API.Models;

namespace RaceDay.Controllers.API.Controllers;

[ApiController]
[Route("api/events")]
[Authorize]
public class EventsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public EventsController(ApplicationDbContext db) => _db = db;

    private int? CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    /// <summary>List all events.</summary>
    [HttpGet]
    public async Task<IActionResult> GetEvents()
    {
        var events = await _db.Events.AsNoTracking().Select(e => new
        {
            e.EventID, e.Name, e.Description, e.Location, e.EventDate, e.EventType, e.RouteDetails, e.WeatherInfo
        }).ToListAsync();

        return Ok(events);
    }

    /// <summary>Get event details with categories.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetEvent(int id)
    {
        var item = await _db.Events.AsNoTracking().Where(e => e.EventID == id).Select(e => new
        {
            e.EventID, e.Name, e.Description, e.Location, e.EventDate, e.EventType, e.RouteDetails, e.WeatherInfo,
            Categories = e.Categories.Select(c => new { c.CategoryID, c.Name, c.Distance, c.AgeRestriction })
        }).SingleOrDefaultAsync();

        return item == null ? NotFound("Event not found.") : Ok(item);
    }

    /// <summary>Create a new event (Organisers only).</summary>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> CreateEvent([FromBody] EventDto dto)
    {
        if (CurrentUserId == null) return Unauthorized();

        var item = new Event
        {
            Name = dto.Name.Trim(),
            Description = dto.Description ?? string.Empty,
            Location = dto.Location.Trim(),
            EventDate = dto.EventDate,
            EventType = dto.EventType.Trim(),
            RouteDetails = dto.RouteDetails ?? string.Empty,
            WeatherInfo = dto.WeatherInfo ?? string.Empty
        };

        _db.Events.Add(item);
        _db.EventOrganisers.Add(new EventOrganiser { Event = item, OrganizerID = CurrentUserId.Value });
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = item.EventID }, new { item.EventID, item.Name });
    }

    /// <summary>Update an existing event (Organisers only).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> UpdateEvent(int id, [FromBody] EventDto dto)
    {
        if (CurrentUserId == null) return Unauthorized();

        var item = await _db.Events.Include(e => e.EventOrganisers).SingleOrDefaultAsync(e => e.EventID == id);
        if (item == null) return NotFound("Event not found.");
        if (!item.EventOrganisers.Any(x => x.OrganizerID == CurrentUserId.Value)) return Forbid();

        item.Name = dto.Name.Trim();
        item.Description = dto.Description ?? string.Empty;
        item.Location = dto.Location.Trim();
        item.EventDate = dto.EventDate;
        item.EventType = dto.EventType.Trim();
        item.RouteDetails = dto.RouteDetails ?? string.Empty;
        item.WeatherInfo = dto.WeatherInfo ?? string.Empty;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Delete an event (Organisers only).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        if (CurrentUserId == null) return Unauthorized();

        var item = await _db.Events.Include(e => e.EventOrganisers)
            .Include(e => e.Categories).ThenInclude(c => c.Registrations)
            .SingleOrDefaultAsync(e => e.EventID == id);

        if (item == null) return NotFound("Event not found.");
        if (!item.EventOrganisers.Any(x => x.OrganizerID == CurrentUserId.Value)) return Forbid();
        if (item.Categories.Any(c => c.Registrations.Count > 0)) return Conflict("Cannot delete an event with existing registrations.");

        _db.Events.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
