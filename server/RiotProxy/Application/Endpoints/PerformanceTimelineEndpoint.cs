using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.PerformanceTimelineDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class PerformanceTimelineEndpoint : IEndpoint
    {
        public string Route { get; }

        public PerformanceTimelineEndpoint(string basePath)
        {
            Route = basePath + "/performance/{userId}";
        }

        public void Configure(WebApplication app)
        {
            app.MapGet(Route, async (
                [FromRoute] string userId,
                [FromQuery] int? limit,
                [FromServices] GamerRepository gamerRepo,
                [FromServices] UserGamerRepository userGamerRepo,
                [FromServices] LolMatchParticipantRepository matchParticipantRepo
            ) =>
            {
                try
                {
                    var gameLimit = limit switch
                    {
                        20 => 20,
                        50 => 50,
                        100 => 100,
                        _ => 100 // Default to 100 games
                    };

                    var userIdInt = int.TryParse(userId, out var result) 
                        ? result 
                        : throw new ArgumentException($"Invalid userId: {userId}");

                    var puuIds = await userGamerRepo.GetGamersPuuIdByUserIdAsync(userIdInt);
                    var distinctPuuIds = (puuIds ?? []).Distinct().ToArray();

                    if (distinctPuuIds.Length == 0)
                    {
                        return Results.Ok(new PerformanceTimelineResponse(Gamers: []));
                    }

                    var gamerTimelines = new List<GamerTimeline>();

                    foreach (var puuId in distinctPuuIds)
                    {
                        var gamer = await gamerRepo.GetByPuuIdAsync(puuId);
                        if (gamer == null)
                        {
                            Console.WriteLine($"Gamer with puuid {puuId} not found in database.");
                            continue;
                        }

                        var gamerName = $"{gamer.GamerName}#{gamer.Tagline}";
                        var matchRecords = await matchParticipantRepo.GetMatchPerformanceTimelineAsync(puuId, gameLimit);

                        var dataPoints = new List<PerformanceDataPoint>();
                        int winsAccumulated = 0;
                        int gameNumber = 0;

                        foreach (var record in matchRecords)
                        {
                            gameNumber++;
                            if (record.Win) winsAccumulated++;

                            var runningWinrate = gameNumber > 0 
                                ? Math.Round((winsAccumulated / (double)gameNumber) * 100, 1) 
                                : 0;

                            var goldPerMin = record.DurationMinutes > 0 
                                ? Math.Round(record.GoldEarned / record.DurationMinutes, 1) 
                                : 0;

                            var csPerMin = record.DurationMinutes > 0 
                                ? Math.Round(record.CreepScore / record.DurationMinutes, 1) 
                                : 0;

                            dataPoints.Add(new PerformanceDataPoint(
                                GameNumber: gameNumber,
                                Winrate: runningWinrate,
                                GoldPerMin: goldPerMin,
                                CsPerMin: csPerMin,
                                Win: record.Win,
                                GameEndTimestamp: record.GameEndTimestamp,
                                Patch: null // Could be extracted from MatchId if needed (e.g., "NA1_12345" doesn't contain patch)
                            ));
                        }

                        gamerTimelines.Add(new GamerTimeline(
                            GamerName: gamerName,
                            DataPoints: dataPoints
                        ));
                    }

                    return Results.Ok(new PerformanceTimelineResponse(Gamers: gamerTimelines));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest(ex is ArgumentException
                        ? "Invalid argument when getting performance timeline"
                        : "Invalid operation when getting performance timeline");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting performance timeline");
                }
            });
        }
    }
}
