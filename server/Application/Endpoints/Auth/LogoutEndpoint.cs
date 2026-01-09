using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace RiotProxy.Application.Endpoints.Auth;

/// <summary>
/// v2 Logout Endpoint
/// Clears the session cookie and signs out the user.
/// </summary>
public sealed class LogoutEndpoint : IEndpoint
{
    public string Route { get; }

    public LogoutEndpoint(string basePath)
    {
        Route = basePath + "/logout";
    }

    public void Configure(WebApplication app)
    {
        app.MapPost(Route, async (HttpContext httpContext) =>
        {
            try
            {
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LogoutEndpoint: {ex.Message}");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });
    }
}
