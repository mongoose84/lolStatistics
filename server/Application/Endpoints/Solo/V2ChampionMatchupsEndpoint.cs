using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories.V2;

namespace RiotProxy.Application.Endpoints.Solo;

/// <summary>
/// v2 Champion Matchups Endpoint
/// Returns champion matchup statistics for the authenticated user's linked Riot accounts.
/// GET /api/v2/solo/champion-matchups?queueId={queueId}
/// </summary>
public sealed class V2ChampionMatchupsEndpoint : IEndpoint
{
    public string Route { get; }

    public V2ChampionMatchupsEndpoint(string basePath)
    {
        Route = basePath + "/solo/champion-matchups";
    }

    public void Configure(WebApplication app)
    {
        app.MapGet(Route, [Authorize] async (
            HttpContext httpContext,
            [FromQuery] int? queueId,
            [FromServices] V2RiotAccountsRepository riotAccountsRepo,
            [FromServices] V2ParticipantsRepository participantsRepo,
            [FromServices] ILogger<V2ChampionMatchupsEndpoint> logger
        ) =>
        {
            try
            {
                var userId = GetUserId(httpContext);
                if (userId == null) return Results.Unauthorized();

                // Get all linked Riot accounts for this user
                var riotAccounts = await riotAccountsRepo.GetByUserIdAsync(userId.Value);
                if (riotAccounts.Count == 0)
                {
                    return Results.Ok(new ChampionMatchupsResponse(Matchups: []));
                }

                var puuids = riotAccounts.Select(a => a.Puuid).Distinct().ToArray();

                // Get all matchup data for the user's puuids
                var matchupRecords = await participantsRepo.GetChampionMatchupsByPuuidsAsync(puuids, queueId);

                // Group by champion + role
                var groupedMatchups = matchupRecords
                    .GroupBy(m => new { m.ChampionId, m.ChampionName, m.Role })
                    .Select(group =>
                    {
                        var totalGames = group.Sum(m => m.GamesPlayed);
                        var totalWins = group.Sum(m => m.Wins);
                        var winrate = totalGames > 0 ? Math.Round((double)totalWins / totalGames * 100, 2) : 0;

                        var opponents = group
                            .Select(m => new OpponentStats(
                                OpponentChampionName: m.OpponentChampionName,
                                OpponentChampionId: m.OpponentChampionId,
                                GamesPlayed: m.GamesPlayed,
                                Wins: m.Wins,
                                Losses: m.GamesPlayed - m.Wins,
                                Winrate: m.GamesPlayed > 0 ? Math.Round((double)m.Wins / m.GamesPlayed * 100, 2) : 0
                            ))
                            .OrderByDescending(o => o.GamesPlayed)
                            .ToList();

                        return new ChampionRoleMatchup(
                            ChampionName: group.Key.ChampionName,
                            ChampionId: group.Key.ChampionId,
                            Role: group.Key.Role,
                            TotalGames: totalGames,
                            TotalWins: totalWins,
                            Winrate: winrate,
                            Opponents: opponents
                        );
                    })
                    .OrderByDescending(m => m.TotalGames)
                    .ToList();

                return Results.Ok(new ChampionMatchupsResponse(Matchups: groupedMatchups));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in V2ChampionMatchupsEndpoint");
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

    // Response DTOs
    public record ChampionMatchupsResponse(
        [property: JsonPropertyName("matchups")] IList<ChampionRoleMatchup> Matchups
    );

    public record ChampionRoleMatchup(
        [property: JsonPropertyName("championName")] string ChampionName,
        [property: JsonPropertyName("championId")] int ChampionId,
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("totalGames")] int TotalGames,
        [property: JsonPropertyName("totalWins")] int TotalWins,
        [property: JsonPropertyName("winrate")] double Winrate,
        [property: JsonPropertyName("opponents")] IList<OpponentStats> Opponents
    );

    public record OpponentStats(
        [property: JsonPropertyName("opponentChampionName")] string OpponentChampionName,
        [property: JsonPropertyName("opponentChampionId")] int OpponentChampionId,
        [property: JsonPropertyName("gamesPlayed")] int GamesPlayed,
        [property: JsonPropertyName("wins")] int Wins,
        [property: JsonPropertyName("losses")] int Losses,
        [property: JsonPropertyName("winrate")] double Winrate
    );
}

