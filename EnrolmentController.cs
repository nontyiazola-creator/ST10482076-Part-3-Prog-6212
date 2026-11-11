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
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayContext _ctx;

    public EnrolmentsController(RaceDayContext ctx)
    {
        _ctx = ctx;
    }

    /// <summary>
    /// Enrols the logged-in Participant in an event under a chosen category.
    /// The enrolment records the participant, the event and the category.
    /// </summary>
    /// <response code="201">Enrolment recorded.</response>
    /// <response code="400">Category does not belong to the event.</response>
    /// <response code="403">Organisers cannot enrol.</response>
    /// <response code="404">Event or category not found.</response>
    /// <response code="409">Already enrolled in this event and category.</response>
    [HttpPost("enrolments")]
    [SessionAuthorize("Participant")]
    public async Task<IActionResult> Enrol(EnrolmentDto dto)
    {
        var participantId = HttpContext.Session.GetInt32("UserID")!.Value;

        if (!await _ctx.Events.AnyAsync(e => e.EventID == dto.EventID))
            return NotFound(new { message = "Event not found." });

        var category = await _ctx.Categories.FindAsync(dto.CategoryID);
        if (category == null)
            return NotFound(new { message = "Category not found." });

        if (category.EventID != dto.EventID)
            return BadRequest(new { message = "That category does not belong to this event." });

        var already = await _ctx.Enrolments.AnyAsync(e =>
            e.ParticipantID == participantId && e.EventID == dto.EventID && e.CategoryID == dto.CategoryID);
        if (already)
            return Conflict(new { message = "You are already enrolled in this event and category." });

        var enrolment = new Enrolment
        {
            ParticipantID = participantId,
            EventID = dto.EventID,
            CategoryID = dto.CategoryID
        };

        _ctx.Enrolments.Add(enrolment);
        await _ctx.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            enrolment.EnrolmentID,
            enrolment.ParticipantID,
            enrolment.EventID,
            enrolment.CategoryID,
            enrolment.EnrolmentDate,
            enrolment.Status
        });
    }

    /// <summary>
    /// Lists the logged-in Participant's own enrolments.
    /// </summary>
    /// <response code="200">List of the participant's enrolments.</response>
    /// <response code="403">Organisers cannot use this endpoint.</response>
    [HttpGet("enrolments/mine")]
    [SessionAuthorize("Participant")]
    public async Task<IActionResult> GetMine()
    {
        var participantId = HttpContext.Session.GetInt32("UserID")!.Value;

        var enrolments = await _ctx.Enrolments
            .Where(e => e.ParticipantID == participantId)
            .Select(e => new
            {
                e.EnrolmentID,
                e.EventID,
                EventName = e.Event.EventName,
                e.CategoryID,
                CategoryName = e.Category.CategoryName,
                e.EnrolmentDate,
                e.Status
            })
            .ToListAsync();

        return Ok(enrolments);
    }

    /// <summary>
    /// Lists all enrolments for an event. Organisers only, and only for their own events.
    /// </summary>
    /// <response code="200">List of enrolments for the event.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{eventId}/enrolments")]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var ev = await _ctx.Events.FindAsync(eventId);
        if (ev == null)
            return NotFound(new { message = "Event not found." });

        if (ev.OrganiserID != HttpContext.Session.GetInt32("UserID"))
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only view enrolments for your own events." });

        var enrolments = await _ctx.Enrolments
            .Where(e => e.EventID == eventId)
            .Select(e => new
            {
                e.EnrolmentID,
                e.ParticipantID,
                ParticipantName = e.Participant.FirstName + " " + e.Participant.LastName,
                e.CategoryID,
                CategoryName = e.Category.CategoryName,
                e.EnrolmentDate,
                e.Status
            })
            .ToListAsync();

        return Ok(enrolments);
    }
}