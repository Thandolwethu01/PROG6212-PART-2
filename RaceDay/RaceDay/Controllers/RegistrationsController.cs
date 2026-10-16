using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data.API.Data;
using RaceDay.DTOs.API.DTOs;
using RaceDay.Models.API.Models;
using System.Data.Entity;
using System.Security.Claims;

namespace RaceDay.Controllers.API.Controllers;
[ApiController]
[Route("api/registrations")]
[Authorize]
public class RegistrationsController : Controller
{
    private readonly ApplicationDbContext _db;

    public RegistrationsController(ApplicationDbContext db) => _db = db;

    private int UserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Enrol the logged-in participant in a category.</summary>
    [HttpPost]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> Enrol(RegistrationDto dto)
    {
        var category = await _db.Categories
            .Include(c => c.Event)
            .SingleOrDefaultAsync(c => c.CategoryID == dto.CategoryID);

        if (category == null)
            return NotFound("Category not found.");

        if (category.Event.EventDate.Date < DateTime.UtcNow.Date)
            return BadRequest("You cannot enrol in a past event.");

        var alreadyRegistered = await _db.Registrations.AnyAsync(r =>
            r.ParticipantID == UserId &&
            r.CategoryID == dto.CategoryID);

        if (alreadyRegistered)
            return Conflict("You are already enrolled in this category.");

        var registration = new Registration
        {
            ParticipantID = UserId,
            CategoryID = dto.CategoryID,
            RegistrationDate = DateTime.UtcNow,
            PaymentStatus = "Pending"
        };

        _db.Registrations.Add(registration);
        await _db.SaveChangesAsync();

        return StatusCode(201, new
        {
            registration.RegistrationID,
            registration.ParticipantID,
            registration.CategoryID,
            registration.RegistrationDate,
            registration.PaymentStatus
        });
    }

    /// <summary>
    /// View all registrations for an event managed by the organiser.
    /// </summary>
    [HttpGet("event/{eventId:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> GetEventRegistrations(int eventId)
    {
        var managed = await _db.EventOrganisers.AnyAsync(x =>
            x.EventID == eventId && x.OrganizerID == UserId);

        if (!managed)
        {
            if (!await _db.Events.AnyAsync(e => e.EventID == eventId))
                return NotFound("Event not found.");

            return Forbid();
        }

        var registrations = await _db.Registrations
            .AsNoTracking()
            .Where(r => r.Category.EventID == eventId)
            .Select(r => new
            {
                r.RegistrationID,
                r.ParticipantID,
                ParticipantName =
                    r.Participant.FirstName + " " + r.Participant.LastName,
                CategoryName = r.Category.Name,
                r.Category.Distance,
                r.RegistrationDate,
                r.PaymentStatus
            })
            .ToListAsync();

        return Ok(registrations);
    }
}

