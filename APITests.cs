using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Controllers;
using RaceDay.API.Dtos;
using Xunit;

namespace RaceDay.API.Test;

public class ApiTests
{
    // ---------- 1. USER REGISTRATION AND LOGIN ----------

    [Fact]
    public async Task Register_ValidParticipant_Returns201AndHashesPassword()
    {
        using var ctx = TestHelper.CreateContext();
        var controller = TestHelper.WithSession(new AuthController(ctx));

        var result = await controller.Register(new RegisterDto
        {
            FirstName = "Sam",
            LastName = "Runner",
            Email = "sam@test.com",
            Password = "Password123",
            Role = "Participant"
        });

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, created.StatusCode);

        var saved = await ctx.Users.SingleAsync();
        Assert.NotEqual("Password123", saved.PasswordHash); // never stored in plain text
        Assert.True(BCrypt.Net.BCrypt.Verify("Password123", saved.PasswordHash));
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        using var ctx = TestHelper.CreateContext();
        await TestHelper.AddUser(ctx, "Participant", "dup@test.com");
        var controller = TestHelper.WithSession(new AuthController(ctx));

        var result = await controller.Register(new RegisterDto
        {
            FirstName = "A",
            LastName = "B",
            Email = "dup@test.com",
            Password = "Password123",
            Role = "Participant"
        });

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Register_InvalidRole_ReturnsBadRequest()
    {
        using var ctx = TestHelper.CreateContext();
        var controller = TestHelper.WithSession(new AuthController(ctx));

        var result = await controller.Register(new RegisterDto
        {
            FirstName = "A",
            LastName = "B",
            Email = "x@test.com",
            Password = "Password123",
            Role = "Admin"
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Login_ValidCredentials_StartsSessionWithUserIdAndRole()
    {
        using var ctx = TestHelper.CreateContext();
        var user = await TestHelper.AddUser(ctx, "Organiser", "org@test.com");
        var controller = TestHelper.WithSession(new AuthController(ctx));

        var result = await controller.Login(new LoginDto { Email = "org@test.com", Password = "Password123" });

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(user.UserID, controller.HttpContext.Session.GetInt32("UserID"));
        Assert.Equal("Organiser", controller.HttpContext.Session.GetString("Role"));
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorizedAndNoSession()
    {
        using var ctx = TestHelper.CreateContext();
        await TestHelper.AddUser(ctx, "Organiser", "org@test.com");
        var controller = TestHelper.WithSession(new AuthController(ctx));

        var result = await controller.Login(new LoginDto { Email = "org@test.com", Password = "WrongPassword" });

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Null(controller.HttpContext.Session.GetInt32("UserID"));
    }

    // ---------- 2. EVENT MANAGEMENT ----------

    [Fact]
    public async Task CreateEvent_ValidData_ReturnsCreatedAndSavesEvent()
    {
        using var ctx = TestHelper.CreateContext();
        var organiser = await TestHelper.AddUser(ctx, "Organiser", "org@test.com");
        var controller = TestHelper.WithSession(new EventsController(ctx), organiser.UserID, "Organiser");

        var result = await controller.Create(new EventDto
        {
            EventName = "Spring Fun Run",
            EventDate = DateTime.Now.AddDays(10),
            Location = "Sandton",
            Distance = 5,
            EventType = "Run"
        });

        Assert.IsType<CreatedAtActionResult>(result);
        var saved = await ctx.Events.SingleAsync();
        Assert.Equal(organiser.UserID, saved.OrganiserID);
    }

    [Fact]
    public async Task CreateEvent_InvalidEventType_ReturnsBadRequest()
    {
        using var ctx = TestHelper.CreateContext();
        var organiser = await TestHelper.AddUser(ctx, "Organiser", "org@test.com");
        var controller = TestHelper.WithSession(new EventsController(ctx), organiser.UserID, "Organiser");

        var result = await controller.Create(new EventDto
        {
            EventName = "Bad Event",
            EventDate = DateTime.Now.AddDays(10),
            Location = "Sandton",
            Distance = 5,
            EventType = "Swim"
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(ctx.Events);
    }

    [Fact]
    public async Task UpdateEvent_OwnEvent_UpdatesSuccessfully()
    {
        using var ctx = TestHelper.CreateContext();
        var organiser = await TestHelper.AddUser(ctx, "Organiser", "org@test.com");
        var (ev, _) = await TestHelper.AddEventWithCategory(ctx, organiser.UserID);
        var controller = TestHelper.WithSession(new EventsController(ctx), organiser.UserID, "Organiser");

        var result = await controller.Update(ev.EventID, new EventDto
        {
            EventName = "Renamed Event",
            EventDate = DateTime.Now.AddDays(20),
            Location = "Pretoria",
            Distance = 21,
            EventType = "Run"
        });

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Renamed Event", (await ctx.Events.FindAsync(ev.EventID))!.EventName);
    }

    [Fact]
    public async Task UpdateEvent_AnotherOrganisersEvent_ReturnsForbidden()
    {
        using var ctx = TestHelper.CreateContext();
        var owner = await TestHelper.AddUser(ctx, "Organiser", "owner@test.com");
        var other = await TestHelper.AddUser(ctx, "Organiser", "other@test.com");
        var (ev, _) = await TestHelper.AddEventWithCategory(ctx, owner.UserID);
        var controller = TestHelper.WithSession(new EventsController(ctx), other.UserID, "Organiser");

        var result = await controller.Update(ev.EventID, new EventDto
        {
            EventName = "Hijacked",
            EventDate = DateTime.Now.AddDays(20),
            Location = "Nowhere",
            Distance = 5,
            EventType = "Run"
        });

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, forbidden.StatusCode);
        Assert.Equal("City 10km", (await ctx.Events.FindAsync(ev.EventID))!.EventName);
    }

    [Fact]
    public async Task DeleteEvent_WithNoEnrolments_DeletesSuccessfully()
    {
        using var ctx = TestHelper.CreateContext();
        var organiser = await TestHelper.AddUser(ctx, "Organiser", "org@test.com");
        var (ev, _) = await TestHelper.AddEventWithCategory(ctx, organiser.UserID);
        var controller = TestHelper.WithSession(new EventsController(ctx), organiser.UserID, "Organiser");

        var result = await controller.Delete(ev.EventID);

        Assert.IsType<OkObjectResult>(result);
        Assert.Empty(ctx.Events);
    }

    [Fact]
    public async Task DeleteEvent_NonExistentEvent_ReturnsNotFound()
    {
        using var ctx = TestHelper.CreateContext();
        var organiser = await TestHelper.AddUser(ctx, "Organiser", "org@test.com");
        var controller = TestHelper.WithSession(new EventsController(ctx), organiser.UserID, "Organiser");

        var result = await controller.Delete(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ---------- 3. ROLE-BASED ACCESS AND AUTHENTICATION ----------

    [Fact]
    public void AuthFilter_NotLoggedIn_Returns401()
    {
        var result = TestHelper.RunAuthFilter(userId: null, role: null);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void AuthFilter_ParticipantOnOrganiserEndpoint_Returns403()
    {
        var result = TestHelper.RunAuthFilter(userId: 1, role: "Participant", "Organiser");

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, forbidden.StatusCode);
    }

    [Fact]
    public void AuthFilter_OrganiserOnParticipantEndpoint_Returns403()
    {
        var result = TestHelper.RunAuthFilter(userId: 1, role: "Organiser", "Participant");

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, forbidden.StatusCode);
    }

    [Fact]
    public void AuthFilter_CorrectRole_IsAllowed()
    {
        var result = TestHelper.RunAuthFilter(userId: 1, role: "Organiser", "Organiser");

        Assert.Null(result);
    }

    [Fact]
    public void AuthFilter_AnyLoggedInUser_IsAllowedWhenNoRoleRequired()
    {
        var result = TestHelper.RunAuthFilter(userId: 2, role: "Participant");

        Assert.Null(result);
    }

    [Fact]
    public void Controllers_ProtectedByAuthFilter()
    {
        // Confirms the endpoints really carry the [SessionAuthorize] attribute
        var eventsAttr = typeof(EventsController)
            .GetCustomAttributes(typeof(RaceDay.API.Filters.SessionAuthorizeAttribute), true);
        var enrolAttr = typeof(EnrolmentsController)
            .GetCustomAttributes(typeof(RaceDay.API.Filters.SessionAuthorizeAttribute), true);

        Assert.NotEmpty(eventsAttr);
        Assert.NotEmpty(enrolAttr);

        var createMethod = typeof(EventsController).GetMethod("Create")!;
        var organiserOnly = createMethod
            .GetCustomAttributes(typeof(RaceDay.API.Filters.SessionAuthorizeAttribute), true);
        Assert.NotEmpty(organiserOnly);
    }

    // ---------- 4. EVENT ENROLMENTS ----------

    [Fact]
    public async Task Enrol_ValidParticipant_RecordsParticipantEventAndCategory()
    {
        using var ctx = TestHelper.CreateContext();
        var organiser = await TestHelper.AddUser(ctx, "Organiser", "org@test.com");
        var participant = await TestHelper.AddUser(ctx, "Participant", "part@test.com");
        var (ev, cat) = await TestHelper.AddEventWithCategory(ctx, organiser.UserID);
        var controller = TestHelper.WithSession(new EnrolmentsController(ctx), participant.UserID, "Participant");

        var result = await controller.Enrol(new EnrolmentDto { EventID = ev.EventID, CategoryID = cat.CategoryID });

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, created.StatusCode);

        var saved = await ctx.Enrolments.SingleAsync();
        Assert.Equal(participant.UserID, saved.ParticipantID);
        Assert.Equal(ev.EventID, saved.EventID);
        Assert.Equal(cat.CategoryID, saved.CategoryID);
    }

    [Fact]
    public async Task Enrol_SameEventAndCategoryTwice_ReturnsConflict()
    {
        using var ctx = TestHelper.CreateContext();
        var organiser = await TestHelper.AddUser(ctx, "Organiser", "org@test.com");
        var participant = await TestHelper.AddUser(ctx, "Participant", "part@test.com");
        var (ev, cat) = await TestHelper.AddEventWithCategory(ctx, organiser.UserID);
        var controller = TestHelper.WithSession(new EnrolmentsController(ctx), participant.UserID, "Participant");
        var dto = new EnrolmentDto { EventID = ev.EventID, CategoryID = cat.CategoryID };

        await controller.Enrol(dto);
        var second = await controller.Enrol(dto);

        Assert.IsType<ConflictObjectResult>(second);
        Assert.Single(ctx.Enrolments);
    }

    [Fact]
    public async Task Enrol_CategoryFromDifferentEvent_ReturnsBadRequest()
    {
        using var ctx = TestHelper.CreateContext();
        var organiser = await TestHelper.AddUser(ctx, "Organiser", "org@test.com");
        var participant = await TestHelper.AddUser(ctx, "Participant", "part@test.com");
        var (ev1, _) = await TestHelper.AddEventWithCategory(ctx, organiser.UserID);
        var (_, cat2) = await TestHelper.AddEventWithCategory(ctx, organiser.UserID);
        var controller = TestHelper.WithSession(new EnrolmentsController(ctx), participant.UserID, "Participant");

        var result = await controller.Enrol(new EnrolmentDto { EventID = ev1.EventID, CategoryID = cat2.CategoryID });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(ctx.Enrolments);
    }

    [Fact]
    public async Task Enrol_NonExistentEvent_ReturnsNotFound()
    {
        using var ctx = TestHelper.CreateContext();
        var participant = await TestHelper.AddUser(ctx, "Participant", "part@test.com");
        var controller = TestHelper.WithSession(new EnrolmentsController(ctx), participant.UserID, "Participant");

        var result = await controller.Enrol(new EnrolmentDto { EventID = 999, CategoryID = 1 });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Organiser_CanViewEnrolmentsForOwnEvent_ButNotForAnotherOrganisersEvent()
    {
        using var ctx = TestHelper.CreateContext();
        var owner = await TestHelper.AddUser(ctx, "Organiser", "owner@test.com");
        var other = await TestHelper.AddUser(ctx, "Organiser", "other@test.com");
        var (ev, _) = await TestHelper.AddEventWithCategory(ctx, owner.UserID);

        var ownerController = TestHelper.WithSession(new EnrolmentsController(ctx), owner.UserID, "Organiser");
        var otherController = TestHelper.WithSession(new EnrolmentsController(ctx), other.UserID, "Organiser");

        Assert.IsType<OkObjectResult>(await ownerController.GetForEvent(ev.EventID));

        var forbidden = Assert.IsType<ObjectResult>(await otherController.GetForEvent(ev.EventID));
        Assert.Equal(403, forbidden.StatusCode);
    }
}