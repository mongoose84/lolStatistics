using Microsoft.AspNetCore.Mvc;
using RiotProxy.Application.DTOs;
using RiotProxy.Infrastructure.External.Database.Repositories;

namespace RiotProxy.Application.Endpoints;

/// <summary>
/// Endpoint for fetching team game duration analysis.
/// </summary>
public class TeamDurationAnalysisEndpoint : IEndpoint
{
    public string Route { get; }

    public TeamDurationAnalysisEndpoint(string basePath)
    {
        Route = basePath + "/team-duration-analysis/{userId}";
    }

    public void Configure(WebApplication app)
    {
        app.MapGet(Route, async (
            [FromRoute] string userId,
            [FromServices] UserGamerRepository userGamerRepo,
            [FromServices] LolMatchParticipantRepository participantRepo) =>
        {
            try
            {
                var userIdInt = int.TryParse(userId, out var result) ? result : throw new ArgumentException($"Invalid userId: {userId}");
                var puuIds = await userGamerRepo.GetGamersPuuIdByUserIdAsync(userIdInt);
                var distinctPuuIds = (puuIds ?? []).Distinct().ToArray();

                if (distinctPuuIds.Length < 3)
                {
                    return Results.BadRequest("Team analytics requires at least 3 players.");
                }

                var durationStats = await participantRepo.GetTeamDurationStatsByPuuIdsAsync(distinctPuuIds);

                var buckets = new List<TeamDurationAnalysisDto.DurationBucket>
                {
                    new("Early Game (< 25 min)", 0, 25, 0, 0, 0),
                    new("Mid Game (25-35 min)", 25, 35, 0, 0, 0),
                    new("Late Game (35+ min)", 35, 999, 0, 0, 0)
                };

                foreach (var stat in durationStats)
                {
                    int idx = stat.DurationBucket switch
                    {
                        "early" => 0,
                        "mid" => 1,
                        "late" => 2,
                        _ => -1
                    };

                    if (idx >= 0)
                    {
                        var bucket = buckets[idx];
                        double winRate = stat.GamesPlayed > 0 ? Math.Round((double)stat.Wins / stat.GamesPlayed * 100, 1) : 0;
                        buckets[idx] = bucket with
                        {
                            GamesPlayed = stat.GamesPlayed,
                            Wins = stat.Wins,
                            WinRate = winRate
                        };
                    }
                }

                // Find best duration
                var bestBucket = buckets
                    .Where(b => b.GamesPlayed >= 3)
                    .OrderByDescending(b => b.WinRate)
                    .FirstOrDefault();

                return Results.Ok(new TeamDurationAnalysisDto.TeamDurationAnalysisResponse(
                    Buckets: buckets,
                    BestDuration: bestBucket?.Label ?? "Not enough data",
                    BestWinRate: bestBucket?.WinRate ?? 0
                ));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return Results.BadRequest(ex.Message);
            }
        });
    }
}

