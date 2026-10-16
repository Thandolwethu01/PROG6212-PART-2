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
[Route("api/results")]
[Authorize]

public class ResultsController : Controller
{
    private readonly ApplicationDbContext _db;

    public ResultsController(ApplicationDbContext db) => _db = db;

    private int UserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Record or update a participant's result for an event you manage.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> SaveResult(ResultDto dto)
    {
        if (dto.FinishTime < TimeSpan.Zero ||
            dto.Position < 1)
            return BadRequest("Invalid finish time or position.");

        var registration = await _db.Registrations
            .Include(r => r.Category)
            .ThenInclude(c => c.Event)
            .ThenInclude(e => e.EventOrganisers)
            .Include(r => r.Result)
            .SingleOrDefaultAsync(r =>
                r.RegistrationID == dto.RegistrationID);

        if (registration == null)
            return NotFound("Registration not found.");

        if (!registration.Category.Event.EventOrganisers.Any(x =>
                x.OrganizerID == UserId))
            return Forbid();

        var result = registration.Result;

        if (result == null)
        {
            result = new Result
            {
                RegistrationID = registration.RegistrationID
            };

            _db.Results.Add(result);
        }

        result.FinishTime = dto.FinishTime;
        result.Position = dto.Position;
        result.SplitTimes = dto.SplitTimes;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            result.ResultID,
            result.RegistrationID,
            result.FinishTime,
            result.Position,
            result.SplitTimes
        });
    }

    /// <summary>View results belonging to the logged-in participant.</summary>
    [HttpGet("my-results")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> MyResults()
    {
        var results = await _db.Results
            .AsNoTracking()
            .Where(r => r.Registration.ParticipantID == UserId)
            .Select(r => new
            {
                r.ResultID,
                r.RegistrationID,
                EventName = r.Registration.Category.Event.Name,
                CategoryName = r.Registration.Category.Name,
                r.FinishTime,
                r.Position,
                r.SplitTimes
            })
            .ToListAsync();

        return Ok(results);
    }
}