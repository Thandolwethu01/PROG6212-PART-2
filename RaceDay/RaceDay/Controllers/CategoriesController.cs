using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data.API.Data;
using RaceDay.DTOs.API.DTOs;
using RaceDay.Models.API.Models;

namespace RaceDay.Controllers.API.Controllers;

[ApiController]
[Route("api/events/{eventId:int}/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public CategoriesController(ApplicationDbContext db) => _db = db;

    private int? CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    /// <summary>Get event categories.</summary>
    [HttpGet]
    public async Task<IActionResult> GetCategories(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventID == eventId)) return NotFound("Event not found.");

        var categories = await _db.Categories.AsNoTracking()
            .Where(c => c.EventID == eventId)
            .Select(c => new { c.CategoryID, c.Name, c.Distance, c.AgeRestriction })
            .ToListAsync();

        return Ok(categories);
    }

    /// <summary>Add category to event (Organiser only).</summary>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> CreateCategory(int eventId, [FromBody] CategoryDto dto)
    {
        if (CurrentUserId == null) return Unauthorized();

        var item = await _db.Events.Include(e => e.EventOrganisers).SingleOrDefaultAsync(e => e.EventID == eventId);
        if (item == null) return NotFound("Event not found.");
        if (!item.EventOrganisers.Any(x => x.OrganizerID == CurrentUserId.Value)) return Forbid();

        var category = new Category
        {
            EventID = eventId,
            Name = dto.Name.Trim(),
            Distance = dto.Distance,
            AgeRestriction = dto.AgeRestriction
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCategories), new { eventId }, new { category.CategoryID, category.Name });
    }

    /// <summary>Update event category (Organiser only).</summary>
    [HttpPut("{categoryId:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> UpdateCategory(int eventId, int categoryId, [FromBody] CategoryDto dto)
    {
        if (CurrentUserId == null) return Unauthorized();

        var category = await _db.Categories.Include(c => c.Event).ThenInclude(e => e.EventOrganisers)
            .SingleOrDefaultAsync(c => c.CategoryID == categoryId && c.EventID == eventId);

        if (category == null) return NotFound("Category not found.");
        if (!category.Event.EventOrganisers.Any(x => x.OrganizerID == CurrentUserId.Value)) return Forbid();

        category.Name = dto.Name.Trim();
        category.Distance = dto.Distance;
        category.AgeRestriction = dto.AgeRestriction;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Delete event category (Organiser only).</summary>
    [HttpDelete("{categoryId:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> DeleteCategory(int eventId, int categoryId)
    {
        if (CurrentUserId == null) return Unauthorized();

        var category = await _db.Categories.Include(c => c.Event).ThenInclude(e => e.EventOrganisers)
            .Include(c => c.Registrations)
            .SingleOrDefaultAsync(c => c.CategoryID == categoryId && c.EventID == eventId);

        if (category == null) return NotFound("Category not found.");
        if (!category.Event.EventOrganisers.Any(x => x.OrganizerID == CurrentUserId.Value)) return Forbid();
        if (category.Registrations.Count > 0) return Conflict("Category cannot be deleted because it has registrations.");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
