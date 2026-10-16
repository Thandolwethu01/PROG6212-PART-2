using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data.API.Data;
using RaceDay.DTOs.API.DTOs;
using RaceDay.Models.API.Models;

namespace RaceDay.Controllers.API.Controllers;

[ApiController]
[Route("api/registrations")]
[Authorize]
public class RegistrationsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public RegistrationsController(ApplicationDbContext db) => _db = db;

    private int? CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    /// <summary>Enrol participant into a category.</summary>
    [HttpPost]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> Enrol([FromBody] RegistrationDto dto)
    {
        if (CurrentUserId == null) return Unauthorized();

        var category = await _db.Categories.Include(c => c.Event).SingleOrDefaultAsync(c => c.CategoryID == dto.CategoryID);
        if (category == null) return NotFound("Category not found.");
        if (category.Event.EventDate.Date < DateTime.UtcNow.Date) return BadRequest("You cannot enrol in a past event.");

        var alreadyRegistered = await _db.Registrations.AnyAsync(r =>
            r.ParticipantID == CurrentUserId.Value && r.CategoryID == dto.CategoryID);
        if (alreadyRegistered) return Conflict("You are already enrolled in this category.");

        var registration = new Registration
        {
            ParticipantID = CurrentUserId.Value,
            CategoryID = dto.CategoryID,
            RegistrationDate = DateTime.UtcNow,
            PaymentStatus = "Pending"
        };

        _db.Registrations.Add(registration);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            registration.RegistrationID, registration.ParticipantID, registration.CategoryID, registration.RegistrationDate, registration.PaymentStatus
        });
    }

    /// <summary>View event registrations (Organisers only).</summary>
    [HttpGet("event/{eventId:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> GetEventRegistrations(int eventId)
    {
        if (CurrentUserId == null) return Unauthorized();

        var managed = await _db.EventOrganisers.AnyAsync(x => x.EventID == eventId && x.OrganizerID == CurrentUserId.Value);
        if (!managed) return Forbid();

        var registrations = await _db.Registrations.AsNoTracking()
            .Where(r => r.Category.EventID == eventId)
            .Select(r => new
            {
                r.RegistrationID,
                r.ParticipantID,
                ParticipantName = r.Participant.FirstName + " " + r.Participant.LastName,
                CategoryName = r.Category.Name,
                r.Category.Distance,
                r.RegistrationDate,
                r.PaymentStatus
            }).ToListAsync();

        return Ok(registrations);
    }
}
