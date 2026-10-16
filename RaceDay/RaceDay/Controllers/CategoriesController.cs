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
[Route("api/events/{eventId:int}/categories")]
[Authorize]
public class CategoriesController : Controller
    {
    private readonly ApplicationDbContext _db;

    public CategoriesController(ApplicationDbContext db) => _db = db;

    private int UserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>View categories belonging to an event.</summary>
    [HttpGet]
    public async Task<IActionResult> GetCategories(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventID == eventId))
            return NotFound("Event not found.");

        var categories = await _db.Categories
            .AsNoTracking()
            .Where(c => c.EventID == eventId)
            .Select(c => new
            {
                c.CategoryID,
                c.Name,
                c.Distance,
                c.AgeRestriction
            })
            .ToListAsync();

        return Ok(categories);
    }

    /// <summary>Create a category for an event managed by the organiser.</summary>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> CreateCategory(
        int eventId, CategoryDto dto)
    {
        var item = await _db.Events
            .Include(e => e.EventOrganisers)
            .SingleOrDefaultAsync(e => e.EventID == eventId);

        if (item == null)
            return NotFound("Event not found.");

        if (!item.EventOrganisers.Any(x => x.OrganizerID == UserId))
            return Forbid();

        var category = new Category
        {
            EventID = eventId,
            Name = dto.Name.Trim(),
            Distance = dto.Distance,
            AgeRestriction = dto.AgeRestriction
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetCategories),
            new { eventId },
            new { category.CategoryID, category.Name });
    }

    /// <summary>Update a category managed by the current organiser.</summary>
    [HttpPut("{categoryId:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> UpdateCategory(
        int eventId, int categoryId, CategoryDto dto)
    {
        var category = await _db.Categories
            .Include(c => c.Event)
            .ThenInclude(e => e.EventOrganisers)
            .SingleOrDefaultAsync(c =>
                c.CategoryID == categoryId &&
                c.EventID == eventId);

        if (category == null)
            return NotFound();

        if (!category.Event.EventOrganisers.Any(x =>
                x.OrganizerID == UserId))
            return Forbid();

        category.Name = dto.Name.Trim();
        category.Distance = dto.Distance;
        category.AgeRestriction = dto.AgeRestriction;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Delete a category if it has no registrations.</summary>
    [HttpDelete("{categoryId:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> DeleteCategory(
        int eventId, int categoryId)
    {
        var category = await _db.Categories
            .Include(c => c.Event)
            .ThenInclude(e => e.EventOrganisers)
            .Include(c => c.Registrations)
            .SingleOrDefaultAsync(c =>
                c.CategoryID == categoryId &&
                c.EventID == eventId);

        if (category == null)
            return NotFound();

        if (!category.Event.EventOrganisers.Any(x =>
                x.OrganizerID == UserId))
            return Forbid();

        if (category.Registrations.Count > 0)
            return Conflict("Category has registrations.");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

