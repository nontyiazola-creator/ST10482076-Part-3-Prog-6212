using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Filters;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api")]
[SessionAuthorize]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayContext _ctx;

    public CategoriesController(RaceDayContext ctx)
    {
        _ctx = ctx;
    }

    /// <summary>
    /// Lists the categories available for an event. Available to both roles.
    /// </summary>
    /// <response code="200">List of categories.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{eventId}/categories")]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await _ctx.Events.AnyAsync(e => e.EventID == eventId))
            return NotFound(new { message = "Event not found." });

        var categories = await _ctx.Categories
            .Where(c => c.EventID == eventId)
            .Select(c => new { c.CategoryID, c.EventID, c.CategoryName, c.Description })
            .ToListAsync();

        return Ok(categories);
    }

    /// <summary>
    /// Adds a category (e.g. Under 20, Senior, 10km) to an event.
    /// Organisers only, and only for events they created.
    /// </summary>
    /// <response code="201">Category created.</response>
    /// <response code="400">Category name missing.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">Category name already exists for this event.</response>
    [HttpPost("events/{eventId}/categories")]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> Create(int eventId, CategoryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CategoryName))
            return BadRequest(new { message = "Category name is required." });

        var ev = await _ctx.Events.FindAsync(eventId);
        if (ev == null)
            return NotFound(new { message = "Event not found." });

        if (ev.OrganiserID != HttpContext.Session.GetInt32("UserID"))
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only add categories to your own events." });

        if (await _ctx.Categories.AnyAsync(c => c.EventID == eventId && c.CategoryName == dto.CategoryName))
            return Conflict(new { message = "This category already exists for the event." });

        var category = new Category
        {
            EventID = eventId,
            CategoryName = dto.CategoryName,
            Description = dto.Description
        };

        _ctx.Categories.Add(category);
        await _ctx.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created,
            new { category.CategoryID, category.EventID, category.CategoryName, category.Description });
    }

    /// <summary>
    /// Updates a category. Organisers only, and only for their own events.
    /// </summary>
    /// <response code="200">Category updated.</response>
    /// <response code="400">Category name missing.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Category not found.</response>
    [HttpPut("categories/{id}")]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> Update(int id, CategoryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CategoryName))
            return BadRequest(new { message = "Category name is required." });

        var category = await _ctx.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
        if (category == null)
            return NotFound(new { message = "Category not found." });

        if (category.Event.OrganiserID != HttpContext.Session.GetInt32("UserID"))
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only edit categories on your own events." });

        category.CategoryName = dto.CategoryName;
        category.Description = dto.Description;
        await _ctx.SaveChangesAsync();

        return Ok(new { message = "Category updated." });
    }

    /// <summary>
    /// Deletes a category. Organisers only, and only for their own events.
    /// Categories that already have enrolments cannot be deleted.
    /// </summary>
    /// <response code="200">Category deleted.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">Category has enrolments.</response>
    [HttpDelete("categories/{id}")]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _ctx.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
        if (category == null)
            return NotFound(new { message = "Category not found." });

        if (category.Event.OrganiserID != HttpContext.Session.GetInt32("UserID"))
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only delete categories on your own events." });

        if (await _ctx.Enrolments.AnyAsync(e => e.CategoryID == id))
            return Conflict(new { message = "This category has enrolments and cannot be deleted." });

        _ctx.Categories.Remove(category);
        await _ctx.SaveChangesAsync();

        return Ok(new { message = "Category deleted." });
    }
}