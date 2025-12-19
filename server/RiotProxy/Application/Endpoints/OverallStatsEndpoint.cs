using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using RiotProxy.Infrastructure.External.Riot;
using RiotProxy.Application.DTOs;
using static RiotProxy.Application.DTOs.OverallStatsDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class OverallStatsEndpoint : IEndpoint
    {
        public string Route { get; }

        public OverallStatsEndpoint(string basePath)
        {
            Route = basePath + "/stats/{userId}";
        }

        public void Configure(WebApplication app)
        {
            app.MapGet(Route, async (
                [FromRoute] string userId,
                [FromServices] GamerRepository gamerRepo,
                [FromServices] UserGamerRepository userGamerRepo,
                [FromServices] LolMatchParticipantRepository matchParticipantRepo,
                [FromServices] IRiotApiClient riotApiClient
                ) =>
            {
                try
                {
                    var userIdInt = int.TryParse(userId, out var result) ? result : throw new ArgumentException($"Invalid userId: {userId}");
                    var puuids = await userGamerRepo.GetGamersPuuidByUserIdAsync(userIdInt);
                    // Optional: touch Riot API for version if needed elsewhere
                    // var lolVersion = await riotApiClient.GetLolVersionAsync();

                    var distinctPuuids = (puuids ?? new List<string>()).Distinct().ToArray();
                    if (distinctPuuids.Length == 0)
                    {
                        var emptyStats = new OverallStatsRequest(GamesPlayed: 0, Wins: 0);
                        return Results.Ok(emptyStats);
                    }

                    // Fetch totals in parallel for better latency
                    var matchCountTasks = distinctPuuids.Select(p => matchParticipantRepo.GetMatchesCountByPuuidAsync(p));
                    var winCountTasks = distinctPuuids.Select(p => matchParticipantRepo.GetWinsByPuuidAsync(p));

                    var matchCounts = await Task.WhenAll(matchCountTasks);
                    var winCounts = await Task.WhenAll(winCountTasks);

                    var totalGames = matchCounts.Sum();
                    var totalWins = winCounts.Sum();

                    var overallStats = new OverallStatsRequest(
                        GamesPlayed: totalGames,
                        Wins: totalWins
                    );
                
                    return Results.Ok(overallStats);
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    return Results.BadRequest("Invalid operation when getting gamers");
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    return Results.BadRequest("Invalid argument when getting gamers");
                }
                catch (Exception ex) when (
                    !(ex is OutOfMemoryException) &&
                    !(ex is StackOverflowException) &&
                    !(ex is ThreadAbortException)
                )
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    return Results.BadRequest("Error when getting gamers");
                }
            });
        }
    }
}