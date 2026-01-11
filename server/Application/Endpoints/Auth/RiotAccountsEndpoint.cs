using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories.V2;
using RiotProxy.Infrastructure.External.Riot;

namespace RiotProxy.Application.Endpoints.Auth;

/// <summary>
/// v2 Riot Accounts Endpoint
/// Provides operations on linked Riot accounts:
/// - DELETE /api/v2/users/me/riot-accounts/{puuid} - Unlink a Riot account
/// - POST /api/v2/users/me/riot-accounts/{puuid}/sync - Trigger a sync
/// - GET /api/v2/users/me/riot-accounts/{puuid}/sync-status - Get sync status
/// </summary>
public sealed class RiotAccountsEndpoint : IEndpoint
{
    public string Route { get; }
    private readonly string _basePath;

    public RiotAccountsEndpoint(string basePath)
    {
        _basePath = basePath;
        Route = basePath + "/users/me/riot-accounts";
    }

    public void Configure(WebApplication app)
    {
        ConfigureDeleteEndpoint(app);
        ConfigureSyncEndpoint(app);
        ConfigureSyncStatusEndpoint(app);
    }

    private void ConfigureDeleteEndpoint(WebApplication app)
    {
        app.MapDelete(Route + "/{puuid}", [Authorize] async (
            string puuid,
            HttpContext httpContext,
            [FromServices] V2RiotAccountsRepository riotAccountsRepo,
            [FromServices] ILogger<RiotAccountsEndpoint> logger
        ) =>
        {
            try
            {
                var userId = GetUserId(httpContext);
                if (userId == null) return Results.Unauthorized();

                // Check if account exists and belongs to user
                var account = await riotAccountsRepo.GetByPuuidAsync(puuid);
                if (account == null || account.UserId != userId)
                {
                    return Results.NotFound(new { error = "Riot account not found", code = "ACCOUNT_NOT_FOUND" });
                }

                // Delete the account
                await riotAccountsRepo.DeleteAsync(puuid, userId.Value);
                logger.LogInformation("Unlinked Riot account {Puuid} from user {UserId}", puuid, userId);

                return Results.NoContent();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting Riot account {Puuid}", puuid);
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });
    }

    private void ConfigureSyncEndpoint(WebApplication app)
    {
        app.MapPost(Route + "/{puuid}/sync", [Authorize] async (
            string puuid,
            HttpContext httpContext,
            [FromServices] V2RiotAccountsRepository riotAccountsRepo,
            [FromServices] IRiotApiClient riotApiClient,
            [FromServices] ILogger<RiotAccountsEndpoint> logger
        ) =>
        {
            try
            {
                var userId = GetUserId(httpContext);
                if (userId == null) return Results.Unauthorized();

                // Check if account exists and belongs to user
                var account = await riotAccountsRepo.GetByPuuidAsync(puuid);
                if (account == null || account.UserId != userId)
                {
                    return Results.NotFound(new { error = "Riot account not found", code = "ACCOUNT_NOT_FOUND" });
                }

                // Set sync status to syncing
                await riotAccountsRepo.UpdateSyncStatusAsync(puuid, "syncing");
                logger.LogInformation("Started sync for Riot account {Puuid}, user {UserId}", puuid, userId);

                // Note: Full match sync would be handled by a background job
                // For now, we just mark it as syncing and it will be picked up
                // In a production implementation, this would queue a background job

                return Results.Accepted($"{Route}/{puuid}/sync-status", new SyncResponse(puuid, "syncing", "Sync started"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error triggering sync for Riot account {Puuid}", puuid);
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });
    }

    private void ConfigureSyncStatusEndpoint(WebApplication app)
    {
        app.MapGet(Route + "/{puuid}/sync-status", [Authorize] async (
            string puuid,
            HttpContext httpContext,
            [FromServices] V2RiotAccountsRepository riotAccountsRepo,
            [FromServices] ILogger<RiotAccountsEndpoint> logger
        ) =>
        {
            try
            {
                var userId = GetUserId(httpContext);
                if (userId == null) return Results.Unauthorized();

                // Check if account exists and belongs to user
                var account = await riotAccountsRepo.GetByPuuidAsync(puuid);
                if (account == null || account.UserId != userId)
                {
                    return Results.NotFound(new { error = "Riot account not found", code = "ACCOUNT_NOT_FOUND" });
                }

                return Results.Ok(new SyncStatusResponse(
                    puuid,
                    account.SyncStatus,
                    account.LastSyncAt
                ));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting sync status for Riot account {Puuid}", puuid);
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });
    }

    private static long? GetUserId(HttpContext httpContext)
    {
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return null;
        return userId;
    }

    public record SyncResponse(
        [property: JsonPropertyName("puuid")] string Puuid,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("message")] string Message
    );

    public record SyncStatusResponse(
        [property: JsonPropertyName("puuid")] string Puuid,
        [property: JsonPropertyName("syncStatus")] string SyncStatus,
        [property: JsonPropertyName("lastSyncAt")] DateTime? LastSyncAt
    );
}

