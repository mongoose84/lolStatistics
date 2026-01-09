using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;

namespace RiotProxy.Application.Endpoints
{
    /// <summary>
    /// Authenticated users endpoint for API v2. Mirrors the v1 users endpoint but requires auth.
    /// </summary>
    public sealed class UsersV2Endpoint : IEndpoint
    {
        public string Route { get; }

        public UsersV2Endpoint(string basePath)
        {
            Route = basePath + "/users";
        }

        public void Configure(WebApplication app)
        {
            app.MapGet(Route, async ([FromServices] IUserRepository userRepo, [FromServices] ILogger<UsersV2Endpoint> logger) =>
            {
                try
                {
                    var users = await userRepo.GetAllUsersAsync();
                    return Results.Ok(users);
                }
                catch (InvalidOperationException ex)
                {
                    logger.LogError(ex, "Invalid operation when getting users");
                    return Results.BadRequest("Unable to retrieve users");
                }
                catch (ArgumentException ex)
                {
                    logger.LogError(ex, "Invalid argument when getting users");
                    return Results.BadRequest("Unable to retrieve users");
                }
                catch (Exception ex) when (
                    !(ex is OutOfMemoryException) &&
                    !(ex is StackOverflowException) &&
                    !(ex is ThreadAbortException)
                )
                {
                    logger.LogError(ex, "Error when getting users");
                    return Results.BadRequest("Unable to retrieve users");
                }
            }).RequireAuthorization();
        }
    }
}