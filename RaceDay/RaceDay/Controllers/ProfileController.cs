using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data.API.Data;
using RaceDay.DTOs.API.DTOs;

namespace RaceDay.Controllers.API.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ProfileController(ApplicationDbContext db) => _db = db;

    private int? CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    /// <summary>Get current user profile information.</summary>
    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        if (CurrentUserId == null) return Unauthorized();

        if (User.IsInRole("Participant"))
        {
            var p = await _db.Participants.AsNoTracking().SingleOrDefaultAsync(x => x.ParticipantID == CurrentUserId.Value);
            return p == null ? NotFound() : Ok(new { p.ParticipantID, p.FirstName, p.LastName, p.Gender, p.DateOfBirth, p.ContactInfo });
        }

        if (User.IsInRole("Organiser"))
        {
            var o = await _db.Organisers.AsNoTracking().SingleOrDefaultAsync(x => x.OrganizerID == CurrentUserId.Value);
            return o == null ? NotFound() : Ok(new { o.OrganizerID, o.Name, o.OrganizationName, o.ContactInfo });
        }

        return Forbid();
    }

    /// <summary>Update profile information.</summary>
    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        if (CurrentUserId == null) return Unauthorized();

        if (User.IsInRole("Participant"))
        {
            var p = await _db.Participants.FindAsync(CurrentUserId.Value);
            if (p == null) return NotFound();

            if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName) ||
                dto.DateOfBirth == null || dto.DateOfBirth.Value.Date > DateTime.UtcNow.Date)
                return BadRequest("Valid participant personal details are required.");

            p.FirstName = dto.FirstName.Trim();
            p.LastName = dto.LastName.Trim();
            p.Gender = dto.Gender?.Trim() ?? string.Empty;
            p.DateOfBirth = dto.DateOfBirth.Value.Date;
        }
        else if (User.IsInRole("Organiser"))
        {
            var o = await _db.Organisers.FindAsync(CurrentUserId.Value);
            if (o == null) return NotFound();

            if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.OrganizationName))
                return BadRequest("Name and organization name are required.");

            o.Name = dto.Name.Trim();
            o.OrganizationName = dto.OrganizationName.Trim();
        }
        else
        {
            return Forbid();
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }
}
