using System.Security.Claims;
using RaceDay.Data.API.Data;
using RaceDay.DTOs.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace RaceDay.Controllers.API.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]

public class ProfileController : Controller
{
    private readonly ApplicationDbContext _db;

    public ProfileController(ApplicationDbContext db) => _db = db;

    private int UserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        if (User.IsInRole("Participant"))
        {
            var p = await _db.Participants.FindAsync(UserId);

            return p == null ? NotFound() : Ok(new
            {
                p.ParticipantID,
                p.FirstName,
                p.LastName,
                p.Gender,
                p.DateOfBirth,
                p.ContactInfo
            });
        }

        if (User.IsInRole("Organiser"))
        {
            var o = await _db.Organisers.FindAsync(UserId);

            return o == null ? NotFound() : Ok(new
            {
                o.OrganizerID,
                o.Name,
                o.OrganizationName,
                o.ContactInfo
            });
        }

        return Forbid();
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
    {
        if (User.IsInRole("Participant"))
        {
            var p = await _db.Participants.FindAsync(UserId);

            if (p == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(dto.FirstName) ||
                string.IsNullOrWhiteSpace(dto.LastName) ||
                dto.DateOfBirth == null ||
                dto.DateOfBirth.Value.Date > DateTime.UtcNow.Date)
                return BadRequest("Valid personal details are required.");

            p.FirstName = dto.FirstName.Trim();
            p.LastName = dto.LastName.Trim();
            p.Gender = dto.Gender.Trim();
            p.DateOfBirth = dto.DateOfBirth.Value.Date;
        }
        else if (User.IsInRole("Organiser"))
        {
            var o = await _db.Organisers.FindAsync(UserId);

            if (o == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(dto.Name) ||
                string.IsNullOrWhiteSpace(dto.OrganizationName))
                return BadRequest("Name and organization are required.");

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
