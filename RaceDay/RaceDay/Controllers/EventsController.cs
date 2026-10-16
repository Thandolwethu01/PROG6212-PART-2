using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data.API.Data;
using RaceDay.DTOs.API.DTOs;
using RaceDay.Models.API.Models;
using System.Data.Entity;
using System.Security.Claims;

namespace RaceDay.Controllers.API.Controllers;

    [ApiController]
[Route("api/events")]
[Authorize]
public class EventsController : Controller
{
    private readonly ApplicationDbContext _db;

    public EventsController(ApplicationDbContext db) => _db = db;

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>List events available to authenticated users.</summary>
    [HttpGet]
    public async Task<IActionResult> GetEvents()
    {
        var events = await _db.Events
            .AsNoTracking()
            .Select(e => new
            {
                e.EventID,
                e.Name,
                e.Description,
                e.Location,
                e.EventDate,
                e.EventType,
                e.RouteDetails,
                e.WeatherInfo
            })
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>Get an event and its categories.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetEvent(int id)
    {
        var item = await _db.Events
            .AsNoTracking()
            .Where(e => e.EventID == id)
            .Select(e => new
            {
                e.EventID,
                e.Name,
                e.Description,
                e.Location,
                e.EventDate,
                e.EventType,
                Categories = e.Categories.Select(c => new
                {
                    c.CategoryID,
                    c.Name,
                    c.Distance,
                    c.AgeRestriction
                })
            })
            .SingleOrDefaultAsync();

        return item == null ? NotFound() : Ok(item);
    }

    /// <summary>Create an event. Organisers only.</summary>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> CreateEvent(EventDto dto)
    {
        if (!ValidEventType(dto.EventType))
            return BadRequest("EventType must be Run, Walk or Cycle.");

        var item = new Event
        {
            Name = dto.Name.Trim(),
            Description = dto.Description,
            Location = dto.Location.Trim(),
            EventDate = dto.EventDate,
            EventType = dto.EventType,
            RouteDetails = dto.RouteDetails,
            WeatherInfo = dto.WeatherInfo
        };

        _db.Events.Add(item);

        _db.EventOrganisers.Add(new EventOrganiser
        {
            Event = item,
            OrganizerID = CurrentUserId
        });

        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent),
            new { id = item.EventID }, new { item.EventID, item.Name });
    }

    /// <summary>Update an event managed by the current organiser.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> UpdateEvent(int id, EventDto dto)
    {
        if (!ValidEventType(dto.EventType))
            return BadRequest("EventType must be Run, Walk or Cycle.");

        var item = await _db.Events
            .Include(e => e.EventOrganisers)
            .SingleOrDefaultAsync(e => e.EventID == id);

        if (item == null)
            return NotFound();

        if (!item.EventOrganisers.Any(x =>
                x.OrganizerID == CurrentUserId))
            return Forbid();

        item.Name = dto.Name.Trim();
        item.Description = dto.Description;
        item.Location = dto.Location.Trim();
        item.EventDate = dto.EventDate;
        item.EventType = dto.EventType;
        item.RouteDetails = dto.RouteDetails;
        item.WeatherInfo = dto.WeatherInfo;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Delete an event managed by the current organiser.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var item = await _db.Events
            .Include(e => e.EventOrganisers)
            .Include(e => e.Categories)
            .ThenInclude(c => c.Registrations)
            .SingleOrDefaultAsync(e => e.EventID == id);

        if (item == null)
            return NotFound();

        if (!item.EventOrganisers.Any(x =>
                x.OrganizerID == CurrentUserId))
            return Forbid();

        if (item.Categories.Any(c => c.Registrations.Count > 0))
            return Conflict(
                "Cannot delete an event with existing registrations.");

        _db.Events.Remove(item);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static bool ValidEventType(string value) =>
        new[] { "Run", "Walk", "Cycle" }
            .Contains(value, StringComparer.OrdinalIgnoreCase);
}