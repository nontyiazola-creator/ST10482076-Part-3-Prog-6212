using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.API.Filters;

/// <summary>
/// Requires a logged-in session. Optionally restricts access to specific roles.
/// [SessionAuthorize] = any logged-in user
/// [SessionAuthorize("Organiser")] = Organisers only
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _roles;

    public SessionAuthorizeAttribute(params string[] roles)
    {
        _roles = roles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var userId = context.HttpContext.Session.GetInt32("UserID");
        var role = context.HttpContext.Session.GetString("Role");

        if (userId == null || role == null)
        {
            context.Result = new UnauthorizedObjectResult(new { message = "You must be logged in." });
            return;
        }

        if (_roles.Length > 0 && !_roles.Contains(role))
        {
            context.Result = new ObjectResult(new { message = "Your role does not have access to this resource." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}