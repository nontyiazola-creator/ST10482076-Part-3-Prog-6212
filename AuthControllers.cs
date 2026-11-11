using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Filters;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayContext _ctx;

    public AuthController(RaceDayContext ctx)
    {
        _ctx = ctx;
    }

    /// <summary>
    /// Registers a new Organiser or Participant account. Passwords are BCrypt-hashed.
    /// </summary>
    /// <response code="201">Account created.</response>
    /// <response code="400">Missing fields or invalid role.</response>
    /// <response code="409">Email already registered.</response>
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password)
            || string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
            return BadRequest(new { message = "All fields are required." });

        if (dto.Role != "Organiser" && dto.Role != "Participant")
            return BadRequest(new { message = "Role must be 'Organiser' or 'Participant'." });

        var exists = await _ctx.Users.AnyAsync(u => u.Email == dto.Email);
        if (exists)
            return Conflict(new { message = "Email already registered." });

        var roleId = dto.Role == "Organiser" ? 1 : 2;

        var user = new User
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            RoleID = roleId
        };

        _ctx.Users.Add(user);
        await _ctx.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created,
            new { user.UserID, user.FirstName, user.LastName, user.Email, dto.Role });
    }

    /// <summary>
    /// Logs in a user and starts a session storing their user ID and role.
    /// </summary>
    /// <response code="200">Login successful; session cookie issued.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _ctx.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid credentials." });

        HttpContext.Session.SetInt32("UserID", user.UserID);
        HttpContext.Session.SetString("Role", user.Role.RoleName);

        return Ok(new { role = user.Role.RoleName, userID = user.UserID });
    }

    /// <summary>
    /// Logs out the current user by clearing their session.
    /// </summary>
    /// <response code="200">Session cleared.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPost("logout")]
    [SessionAuthorize]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new { message = "Logged out." });
    }
}