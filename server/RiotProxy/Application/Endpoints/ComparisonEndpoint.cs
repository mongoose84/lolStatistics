using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.ComparisionDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class OverallStatsEndpoint : IEndpoint
    {
        public string Route { get; }

        public OverallStatsEndpoint(string basePath)
        {
            Route = basePath + "/comparison/{userId}";
        }

        public void Configure(WebApplication app)
        {
            app.MapGet(Route, async (
                [FromRoute] string userId,
                [FromServices] GamerRepository gamerRepo,
                [FromServices] UserGamerRepository userGamerRepo,
                [FromServices] LolMatchParticipantRepository matchParticipantRepo
                ) =>
            {
                try
                {
                    var emptyComparisonRequest = new ComparisonRequest(
                        Winrate: [],
                        Kda: [],
                        CsPrMin: [],
                        GoldPrMin: [],
                        GamesPlayed: []
                    );

                    var userIdInt = int.TryParse(userId, out var result) ? result : throw new ArgumentException($"Invalid userId: {userId}");
                    var puuids = await userGamerRepo.GetGamersPuuidByUserIdAsync(userIdInt);
                    var distinctPuuids = (puuids ?? []).Distinct().ToArray();
                    
                    if (distinctPuuids.Length == 0)
                    {
                        return Results.Ok(emptyComparisonRequest);
                    }
                    var winrateRecords = new List<GamerRecord>();
                    var kdaRecords = new List<GamerRecord>();
                    var csPrMinRecords = new List<GamerRecord>();
                    var goldPrMinRecords = new List<GamerRecord>();
                    var gamesPlayedRecords = new List<GamerRecord>();

                    foreach (var puuid in distinctPuuids)
                    {
                        var gamer = await gamerRepo.GetByPuuidAsync(puuid);
                        if (gamer == null)
                        {
                            Console.WriteLine($"Gamer with puuid {puuid} not found in database.");
                            continue;
                        }

                        var gamerName = $"{gamer.GamerName}#{gamer.Tagline}";
                        var totalDurationMinutes = await matchParticipantRepo.GetTotalDurationPlayedByPuuidAsync(puuid) / 60.0;

                        var winrate = await GetWinrateAsync(matchParticipantRepo, puuid);
                        winrateRecords.Add(new GamerRecord(winrate, gamerName));
                        var kda = await GetKdaAsync(matchParticipantRepo, puuid);
                        kdaRecords.Add(new GamerRecord(kda, gamerName));
                        var csPrMin = await GetCsPrMinAsync(matchParticipantRepo, puuid, totalDurationMinutes);
                        csPrMinRecords.Add(new GamerRecord(csPrMin, gamerName));
                        var goldPrMin = await GetGoldPrMinAsync(matchParticipantRepo, puuid, totalDurationMinutes);
                        goldPrMinRecords.Add(new GamerRecord(goldPrMin, gamerName));
                        var gamesPlayed = await matchParticipantRepo.GetMatchesCountByPuuidAsync(puuid);
                        gamesPlayedRecords.Add(new GamerRecord(gamesPlayed, gamerName));
                    }
                    
                    var comparisonRequest = new ComparisonRequest(
                        Winrate: winrateRecords.OrderByDescending(r => r.Value).ToList(),
                        Kda: kdaRecords.OrderByDescending(r => r.Value).ToList(),
                        CsPrMin:  csPrMinRecords.OrderByDescending(r => r.Value).ToList(),
                        GoldPrMin: goldPrMinRecords.OrderByDescending(r => r.Value).ToList(),
                        GamesPlayed: gamesPlayedRecords.OrderByDescending(r => r.Value).ToList()
                    );

                    return Results.Ok(comparisonRequest);
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest(ex is ArgumentException 
                        ? "Invalid argument when getting gamers" 
                        : "Invalid operation when getting gamers");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting gamers");
                }
            });
        }

        private async Task<double> GetWinrateAsync(LolMatchParticipantRepository repo, string puuId)
        {
            var totalMatches = await repo.GetMatchesCountByPuuidAsync(puuId);
            if (totalMatches == 0) return 0;
            
            var wins = await repo.GetWinsByPuuidAsync(puuId);
            return (double)wins / totalMatches * 100;
        }

        private async Task<double> GetKdaAsync(LolMatchParticipantRepository repo, string puuId)
        {
            var (kills, deaths, assists) = (
                await repo.GetTotalKillsByPuuidAsync(puuId),
                await repo.GetTotalDeathsByPuuidAsync(puuId),
                await repo.GetTotalAssistsByPuuidAsync(puuId)
            );
            
            return deaths == 0 ? 0 : (double)(kills + assists) / deaths;
        }
        private async Task<double> GetCsPrMinAsync(LolMatchParticipantRepository repo, string puuId, double totalDurationMinutes)
        {
            var totalCreepScore = await repo.GetTotalCreepScoreByPuuidAsync(puuId);
            return totalDurationMinutes == 0 ? 0 : totalCreepScore / totalDurationMinutes;
        }

        private async Task<double> GetGoldPrMinAsync(LolMatchParticipantRepository repo, string puuId, double totalDurationMinutes)
        {
            var totalGoldEarned = await repo.GetTotalGoldEarnedByPuuidAsync(puuId);
            return totalDurationMinutes == 0 ? 0 : totalGoldEarned / totalDurationMinutes;
        }
        
    }
}