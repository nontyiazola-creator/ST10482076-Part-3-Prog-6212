using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Filters;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/profile")]
[SessionAuthorize]
public class ProfileController : ControllerBase
{
    private readonly RaceDayContext _ctx;

    public ProfileController(RaceDayContext ctx)
    {
        _ctx = ctx;
    }

    /// <summary>
    /// Returns the logged-in user's own profile. Available to both roles.
    /// </summary>
    /// <response code="200">The user's profile.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var userId = HttpContext.Session.GetInt32("UserID")!.Value;

        var profile = await _ctx.Users
            .Where(u => u.UserID == userId)
            .Select(u => new
            {
                u.UserID,
                u.FirstName,
                u.LastName,
                u.Email,
                Role = u.Role.RoleName,
                u.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (profile == null)
            return NotFound(new { message = "User not found." });

        return Ok(profile);
    }

    /// <summary>
    /// Updates the logged-in user's first name, last name and email.
    /// </summary>
    /// <response code="200">Profile updated.</response>
    /// <response code="400">Missing fields.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="409">Email already used by another account.</response>
    [HttpPut]
    public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName)
            || string.IsNullOrWhiteSpace(dto.Email))
            return BadRequest(new { message = "First name, last name and email are required." });

        var userId = HttpContext.Session.GetInt32("UserID")!.Value;

        var user = await _ctx.Users.FindAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found." });

        var emailTaken = await _ctx.Users.AnyAsync(u => u.Email == dto.Email && u.UserID != userId);
        if (emailTaken)
            return Conflict(new { message = "That email is already used by another account." });

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.Email = dto.Email;
        await _ctx.SaveChangesAsync();

        return Ok(new { message = "Profile updated." });
    }
}