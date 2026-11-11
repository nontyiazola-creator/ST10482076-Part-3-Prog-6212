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
public class ResultsController : ControllerBase
{
    private readonly RaceDayContext _ctx;

    public ResultsController(RaceDayContext ctx)
    {
        _ctx = ctx;
    }

    /// <summary>
    /// Captures or updates the finish time and position for an enrolment.
    /// Organisers only, and only for enrolments on their own events.
    /// </summary>
    /// <response code="200">Result saved.</response>
    /// <response code="400">Neither a finish time nor a position was supplied, or values are invalid.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Enrolment not found.</response>
    [HttpPut("enrolments/{enrolmentId}/result")]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> Capture(int enrolmentId, ResultDto dto)
    {
        if (dto.FinishTime == null && dto.FinishPosition == null)
            return BadRequest(new { message = "Provide a finish time, a finish position, or both." });

        if (dto.FinishTime != null && dto.FinishTime <= TimeSpan.Zero)
            return BadRequest(new { message = "Finish time must be greater than zero." });

        if (dto.FinishPosition != null && dto.FinishPosition <= 0)
            return BadRequest(new { message = "Finish position must be greater than zero." });

        var enrolment = await _ctx.Enrolments
            .Include(e => e.Event)
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EnrolmentID == enrolmentId);

        if (enrolment == null)
            return NotFound(new { message = "Enrolment not found." });

        if (enrolment.Event.OrganiserID != HttpContext.Session.GetInt32("UserID"))
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only capture results for your own events." });

        if (enrolment.Result == null)
        {
            enrolment.Result = new Result
            {
                EnrolmentID = enrolmentId,
                FinishTime = dto.FinishTime,
                FinishPosition = dto.FinishPosition
            };
        }
        else
        {
            enrolment.Result.FinishTime = dto.FinishTime;
            enrolment.Result.FinishPosition = dto.FinishPosition;
            enrolment.Result.RecordedAt = DateTime.Now;
        }

        await _ctx.SaveChangesAsync();

        return Ok(new
        {
            enrolment.Result.ResultID,
            enrolment.EnrolmentID,
            enrolment.Result.FinishTime,
            enrolment.Result.FinishPosition,
            enrolment.Result.RecordedAt
        });
    }

    /// <summary>
    /// Lists all results for an event. Organisers only, and only for their own events.
    /// </summary>
    /// <response code="200">List of results for the event.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{eventId}/results")]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var ev = await _ctx.Events.FindAsync(eventId);
        if (ev == null)
            return NotFound(new { message = "Event not found." });

        if (ev.OrganiserID != HttpContext.Session.GetInt32("UserID"))
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only view results for your own events." });

        var results = await _ctx.Enrolments
            .Where(e => e.EventID == eventId && e.Result != null)
            .Select(e => new
            {
                e.Result!.ResultID,
                e.EnrolmentID,
                ParticipantName = e.Participant.FirstName + " " + e.Participant.LastName,
                CategoryName = e.Category.CategoryName,
                e.Result.FinishTime,
                e.Result.FinishPosition,
                e.Result.RecordedAt
            })
            .ToListAsync();

        return Ok(results);
    }

    /// <summary>
    /// Lists the logged-in Participant's own results across all events.
    /// </summary>
    /// <response code="200">List of the participant's results.</response>
    /// <response code="403">Organisers cannot use this endpoint.</response>
    [HttpGet("results/mine")]
    [SessionAuthorize("Participant")]
    public async Task<IActionResult> GetMine()
    {
        var participantId = HttpContext.Session.GetInt32("UserID")!.Value;

        var results = await _ctx.Enrolments
            .Where(e => e.ParticipantID == participantId && e.Result != null)
            .Select(e => new
            {
                e.Result!.ResultID,
                e.EnrolmentID,
                EventName = e.Event.EventName,
                CategoryName = e.Category.CategoryName,
                e.Result.FinishTime,
                e.Result.FinishPosition,
                e.Result.RecordedAt
            })
            .ToListAsync();

        return Ok(results);
    }
}