using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RiotProxy.External.Domain.Entities.V2;
using RiotProxy.Infrastructure.External.Database.Repositories.V2;
using RiotProxy.Infrastructure.External.Riot;

namespace RiotProxy.Application.Endpoints.Auth;

/// <summary>
/// v2 Game Account Endpoint
/// Links a game account (e.g., Riot account) to the authenticated user.
/// POST /api/v2/users/me/game-account
/// </summary>
public sealed class GameAccountEndpoint : IEndpoint
{
    public string Route { get; }

    public GameAccountEndpoint(string basePath)
    {
        Route = basePath + "/users/me/game-account";
    }

    public void Configure(WebApplication app)
    {
        app.MapPost(Route, [Authorize] async (
            HttpContext httpContext,
            [FromBody] LinkGameAccountRequest request,
            [FromServices] V2UsersRepository usersRepo,
            [FromServices] V2RiotAccountsRepository riotAccountsRepo,
            [FromServices] IRiotApiClient riotApiClient,
            [FromServices] ILogger<GameAccountEndpoint> logger
        ) =>
        {
            try
            {
                // Get current user ID from claims
                var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
                {
                    return Results.Unauthorized();
                }

                // Validate request
                if (string.IsNullOrWhiteSpace(request.Game) || !request.Game.Equals("lol", StringComparison.OrdinalIgnoreCase))
                {
                    return Results.BadRequest(new { error = "Only 'lol' game is supported", code = "UNSUPPORTED_GAME" });
                }

                if (request.GameInfo == null)
                {
                    return Results.BadRequest(new { error = "Game info is required", code = "GAME_INFO_REQUIRED" });
                }

                if (string.IsNullOrWhiteSpace(request.GameInfo.GameName))
                {
                    return Results.BadRequest(new { error = "Game name is required", code = "GAME_NAME_REQUIRED" });
                }

                if (string.IsNullOrWhiteSpace(request.GameInfo.TagLine))
                {
                    return Results.BadRequest(new { error = "Tag line is required", code = "TAG_LINE_REQUIRED" });
                }

                if (string.IsNullOrWhiteSpace(request.GameInfo.Region))
                {
                    return Results.BadRequest(new { error = "Region is required", code = "REGION_REQUIRED" });
                }

                // Validate region
                var validRegions = new[] { "na1", "euw1", "eun1", "kr", "jp1", "br1", "la1", "la2", "oc1", "tr1", "ru", "ph2", "sg2", "th2", "tw2", "vn2" };
                if (!validRegions.Contains(request.GameInfo.Region.ToLowerInvariant()))
                {
                    return Results.BadRequest(new { error = $"Invalid region. Valid regions: {string.Join(", ", validRegions)}", code = "INVALID_REGION" });
                }

                // Verify user exists and is active
                var user = await usersRepo.GetByIdAsync(userId);
                if (user == null || !user.IsActive)
                {
                    return Results.Unauthorized();
                }

                // Lookup PUUID from Riot API
                string puuid;
                try
                {
                    puuid = await riotApiClient.GetPuuIdAsync(request.GameInfo.GameName, request.GameInfo.TagLine);
                }
                catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    logger.LogWarning("Riot account not found: {GameName}#{TagLine}", request.GameInfo.GameName, request.GameInfo.TagLine);
                    return Results.NotFound(new { error = "Riot account not found", code = "RIOT_ACCOUNT_NOT_FOUND" });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error looking up Riot account: {GameName}#{TagLine}", request.GameInfo.GameName, request.GameInfo.TagLine);
                    return Results.Json(new { error = "Failed to verify Riot account", code = "RIOT_API_ERROR" }, statusCode: 503);
                }

                // Check if account is already linked to another user
                var existingAccount = await riotAccountsRepo.GetByPuuidAsync(puuid);
                if (existingAccount != null && existingAccount.UserId != userId)
                {
                    logger.LogWarning("Attempted to link already-linked account. PUUID: {Puuid}, existing user: {ExistingUserId}, requesting user: {UserId}",
                        puuid, existingAccount.UserId, userId);
                    return Results.Conflict(new { error = "This Riot account is already linked to another user", code = "ACCOUNT_ALREADY_LINKED" });
                }

                // Check if this is the user's first account (to set as primary)
                var existingAccounts = await riotAccountsRepo.GetByUserIdAsync(userId);
                var isPrimary = existingAccounts.Count == 0;

                // Create the riot account record
                var riotAccount = new V2RiotAccount
                {
                    Puuid = puuid,
                    UserId = userId,
                    GameName = request.GameInfo.GameName,
                    TagLine = request.GameInfo.TagLine,
                    SummonerName = $"{request.GameInfo.GameName}#{request.GameInfo.TagLine}",
                    Region = request.GameInfo.Region.ToLowerInvariant(),
                    IsPrimary = isPrimary,
                    SyncStatus = "pending",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await riotAccountsRepo.UpsertAsync(riotAccount);
                logger.LogInformation("Linked Riot account {GameName}#{TagLine} (PUUID: {Puuid}) to user {UserId}",
                    request.GameInfo.GameName, request.GameInfo.TagLine, puuid, userId);

                return Results.Created($"{Route}/{puuid}", new LinkGameAccountResponse(
                    puuid,
                    request.GameInfo.GameName,
                    request.GameInfo.TagLine,
                    request.GameInfo.Region.ToLowerInvariant(),
                    isPrimary,
                    "pending"
                ));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in GameAccountEndpoint");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });
    }

    public record LinkGameAccountRequest(
        [property: JsonPropertyName("game")] string Game,
        [property: JsonPropertyName("gameInfo")] GameInfoRequest? GameInfo
    );

    public record GameInfoRequest(
        [property: JsonPropertyName("gameName")] string GameName,
        [property: JsonPropertyName("tagLine")] string TagLine,
        [property: JsonPropertyName("region")] string Region
    );

    public record LinkGameAccountResponse(
        [property: JsonPropertyName("puuid")] string Puuid,
        [property: JsonPropertyName("gameName")] string GameName,
        [property: JsonPropertyName("tagLine")] string TagLine,
        [property: JsonPropertyName("region")] string Region,
        [property: JsonPropertyName("isPrimary")] bool IsPrimary,
        [property: JsonPropertyName("syncStatus")] string SyncStatus
    );
}

