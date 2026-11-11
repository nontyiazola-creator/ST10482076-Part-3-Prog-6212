using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Filters;
using RaceDay.API.Models;

namespace RaceDay.API.Test;

/// <summary>
/// A fake session so controllers can be tested without a running web server.
/// </summary>
public class FakeSession : ISession
{
    private readonly Dictionary<string, byte[]> _store = new();

    public IEnumerable<string> Keys => _store.Keys;
    public string Id { get; } = Guid.NewGuid().ToString();
    public bool IsAvailable => true;

    public void Clear() => _store.Clear();
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Remove(string key) => _store.Remove(key);
    public void Set(string key, byte[] value) => _store[key] = value;
    public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
}

public static class TestHelper
{
    /// <summary>Creates a fresh, empty in-memory database for each test.</summary>
    public static RaceDayContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<RaceDayContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var ctx = new RaceDayContext(options);
        ctx.Database.EnsureCreated(); // also applies the seeded Roles (1 = Organiser, 2 = Participant)
        return ctx;
    }

    /// <summary>Gives a controller an HTTP context with a session, optionally already logged in.</summary>
    public static T WithSession<T>(T controller, int? userId = null, string? role = null) where T : ControllerBase
    {
        var session = new FakeSession();
        if (userId != null) session.SetInt32("UserID", userId.Value);
        if (role != null) session.SetString("Role", role);

        var httpContext = new DefaultHttpContext { Session = session };
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    /// <summary>
    /// Runs the [SessionAuthorize] filter for the given session and returns the
    /// result it produces (null means access was allowed).
    /// </summary>
    public static IActionResult? RunAuthFilter(int? userId, string? role, params string[] allowedRoles)
    {
        var session = new FakeSession();
        if (userId != null) session.SetInt32("UserID", userId.Value);
        if (role != null) session.SetString("Role", role);

        var httpContext = new DefaultHttpContext { Session = session };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());

        new SessionAuthorizeAttribute(allowedRoles).OnAuthorization(context);
        return context.Result;
    }

    public static async Task<User> AddUser(RaceDayContext ctx, string role, string email)
    {
        var user = new User
        {
            FirstName = "Test",
            LastName = role,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123"),
            RoleID = role == "Organiser" ? 1 : 2
        };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();
        return user;
    }

    public static async Task<(Event ev, Category cat)> AddEventWithCategory(RaceDayContext ctx, int organiserId)
    {
        var ev = new Event
        {
            EventName = "City 10km",
            EventDate = DateTime.Now.AddDays(30),
            Location = "Johannesburg",
            Distance = 10,
            EventType = "Run",
            OrganiserID = organiserId
        };
        ctx.Events.Add(ev);
        await ctx.SaveChangesAsync();

        var cat = new Category { EventID = ev.EventID, CategoryName = "Senior" };
        ctx.Categories.Add(cat);
        await ctx.SaveChangesAsync();

        return (ev, cat);
    }
}