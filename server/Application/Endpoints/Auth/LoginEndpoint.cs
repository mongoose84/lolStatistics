using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.LoginDto;

namespace RiotProxy.Application.Endpoints.Auth;

/// <summary>
/// v2 Login Endpoint
/// Validates username/password and sets an httpOnly session cookie for subsequent requests.
/// </summary>
public sealed class LoginEndpoint : IEndpoint
{
    public string Route { get; }

    public LoginEndpoint(string basePath)
    {
        Route = basePath + "/login";
    }

    public void Configure(WebApplication app)
    {
        app.MapPost(Route, async (
            [FromBody] LoginRequest request,
            HttpContext httpContext,
            [FromServices] UserRepository userRepo,
            [FromServices] ILogger<LoginEndpoint> logger
        ) =>
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                    return Results.BadRequest(new { error = "Username and password are required" });

                // Fetch user by username
                var user = await userRepo.GetByUserNameAsync(request.Username);
                if (user == null)
                {
                    logger.LogWarning("Login attempt with non-existent username: {Username}", request.Username);
                    return Results.Unauthorized();
                }

                // For now: simple password validation (in production, use bcrypt/argon2 hash comparison)
                // TODO: Update User table schema to include password_hash column
                // For MVP, we allow any user with a valid username to login
                if (!ValidatePassword(request.Password))
                {
                    logger.LogWarning("Login attempt with invalid password for username: {Username}", request.Username);
                    return Results.Unauthorized();
                }

                // Create claims identity for cookie auth
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.UserName)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
                };

                // Sign in user with cookie
                await httpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties
                );

                logger.LogInformation("User {Username} (ID: {UserId}) logged in successfully", user.UserName, user.UserId);

                return Results.Ok(new LoginResponse(
                    user.UserId,
                    user.UserName,
                    "Login successful"
                ));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in LoginEndpoint");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });
    }

    /// <summary>
    /// Validates password (placeholder for now).
    /// TODO: Replace with bcrypt/argon2 hash comparison once User table has password_hash column.
    /// </summary>
    private static bool ValidatePassword(string password)
    {
        // For MVP: accept any non-empty password
        // In production: compare bcrypt hash of password against user.PasswordHash
        return !string.IsNullOrWhiteSpace(password);
    }
}
