using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data.API.Data;
using RaceDay.DTOs.API.DTOs;
using RaceDay.Models.API.Models;

namespace RaceDay.Controllers.API.Controllers;

[ApiController]
[Route("api/results")]
[Authorize]
public class ResultsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ResultsController(ApplicationDbContext db) => _db = db;

    private int? CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    /// <summary>Record or update race result (Organisers only).</summary>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> SaveResult([FromBody] ResultDto dto)
    {
        if (CurrentUserId == null) return Unauthorized();
        if (dto.FinishTime < TimeSpan.Zero || dto.Position < 1) return BadRequest("Invalid finish time or position.");

        var registration = await _db.Registrations
            .Include(r => r.Category).ThenInclude(c => c.Event).ThenInclude(e => e.EventOrganisers)
            .Include(r => r.Result)
            .SingleOrDefaultAsync(r => r.RegistrationID == dto.RegistrationID);

        if (registration == null) return NotFound("Registration not found.");
        if (!registration.Category.Event.EventOrganisers.Any(x => x.OrganizerID == CurrentUserId.Value)) return Forbid();

        var result = registration.Result;
        if (result == null)
        {
            result = new Result { RegistrationID = registration.RegistrationID };
            _db.Results.Add(result);
        }

        result.FinishTime = dto.FinishTime;
        result.Position = dto.Position;
        result.SplitTimes = dto.SplitTimes;

        await _db.SaveChangesAsync();

        return Ok(new { result.ResultID, result.RegistrationID, result.FinishTime, result.Position, result.SplitTimes });
    }

    /// <summary>View personal results (Participants only).</summary>
    [HttpGet("my-results")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> MyResults()
    {
        if (CurrentUserId == null) return Unauthorized();

        var results = await _db.Results.AsNoTracking()
            .Where(r => r.Registration.ParticipantID == CurrentUserId.Value)
            .Select(r => new
            {
                r.ResultID,
                r.RegistrationID,
                EventName = r.Registration.Category.Event.Name,
                CategoryName = r.Registration.Category.Name,
                r.FinishTime,
                r.Position,
                r.SplitTimes
            }).ToListAsync();

        return Ok(results);
    }
}
