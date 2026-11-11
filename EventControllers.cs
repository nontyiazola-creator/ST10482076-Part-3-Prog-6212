using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Filters;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/events")]
[SessionAuthorize]
public class EventsController : ControllerBase
{
    private readonly RaceDayContext _ctx;
    private static readonly string[] ValidTypes = { "Run", "Walk", "Cycle" };

    public EventsController(RaceDayContext ctx)
    {
        _ctx = ctx;
    }

    /// <summary>
    /// Lists all events. Available to both Organisers and Participants.
    /// </summary>
    /// <response code="200">List of events.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var events = await _ctx.Events
            .Select(e => new
            {
                e.EventID,
                e.EventName,
                e.Description,
                e.EventDate,
                e.Location,
                e.Distance,
                e.EventType,
                e.OrganiserID
            })
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>
    /// Gets a single event by ID. Available to both roles.
    /// </summary>
    /// <response code="200">The event.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var e = await _ctx.Events
            .Where(x => x.EventID == id)
            .Select(x => new
            {
                x.EventID,
                x.EventName,
                x.Description,
                x.EventDate,
                x.Location,
                x.Distance,
                x.EventType,
                x.OrganiserID
            })
            .FirstOrDefaultAsync();

        if (e == null)
            return NotFound(new { message = "Event not found." });

        return Ok(e);
    }

    /// <summary>
    /// Creates a new event. Organisers only.
    /// Expects: name, description, date, location, distance and type (Run, Walk or Cycle).
    /// </summary>
    /// <response code="201">Event created.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="403">Participants cannot create events.</response>
    [HttpPost]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> Create(EventDto dto)
    {
        var error = Validate(dto);
        if (error != null)
            return BadRequest(new { message = error });

        var ev = new Event
        {
            EventName = dto.EventName,
            Description = dto.Description,
            EventDate = dto.EventDate,
            Location = dto.Location,
            Distance = dto.Distance,
            EventType = dto.EventType,
            OrganiserID = HttpContext.Session.GetInt32("UserID")!.Value
        };

        _ctx.Events.Add(ev);
        await _ctx.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = ev.EventID },
            new { ev.EventID, ev.EventName, ev.EventDate, ev.Location, ev.Distance, ev.EventType });
    }

    /// <summary>
    /// Updates an event. Organisers only, and only for events they created.
    /// </summary>
    /// <response code="200">Event updated.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Event not found.</response>
    [HttpPut("{id}")]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> Update(int id, EventDto dto)
    {
        var error = Validate(dto);
        if (error != null)
            return BadRequest(new { message = error });

        var ev = await _ctx.Events.FindAsync(id);
        if (ev == null)
            return NotFound(new { message = "Event not found." });

        if (ev.OrganiserID != HttpContext.Session.GetInt32("UserID"))
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only edit your own events." });

        ev.EventName = dto.EventName;
        ev.Description = dto.Description;
        ev.EventDate = dto.EventDate;
        ev.Location = dto.Location;
        ev.Distance = dto.Distance;
        ev.EventType = dto.EventType;

        await _ctx.SaveChangesAsync();
        return Ok(new { message = "Event updated." });
    }

    /// <summary>
    /// Deletes an event. Organisers only, and only for events they created.
    /// Events that already have enrolments cannot be deleted.
    /// </summary>
    /// <response code="200">Event deleted.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">Event has enrolments.</response>
    [HttpDelete("{id}")]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _ctx.Events
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventID == id);

        if (ev == null)
            return NotFound(new { message = "Event not found." });

        if (ev.OrganiserID != HttpContext.Session.GetInt32("UserID"))
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only delete your own events." });

        if (await _ctx.Enrolments.AnyAsync(en => en.EventID == id))
            return Conflict(new { message = "This event has enrolments and cannot be deleted." });

        _ctx.Categories.RemoveRange(ev.Categories);
        _ctx.Events.Remove(ev);
        await _ctx.SaveChangesAsync();

        return Ok(new { message = "Event deleted." });
    }

    private static string? Validate(EventDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.EventName) || string.IsNullOrWhiteSpace(dto.Location))
            return "Event name and location are required.";
        if (dto.Distance <= 0)
            return "Distance must be greater than zero.";
        if (!ValidTypes.Contains(dto.EventType))
            return "Event type must be Run, Walk or Cycle.";
        return null;
    }
}