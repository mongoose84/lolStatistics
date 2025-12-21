using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using RiotProxy.Infrastructure.External.Riot;
using RiotProxy.Application.DTOs;
using static RiotProxy.Application.DTOs.ComparisionDto;
using RiotProxy.External.Domain.Entities;

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
                            Winrate: new Winrate(0, ""),
                            Kda: new Kda(0, ""),
                            CsPrMin: new CsPrMin(0, ""),
                            GoldPrMin: new GoldPrMin(0, ""),
                            GamesPlayed: new GamesPlayed(0, "")
                        );

                    var userIdInt = int.TryParse(userId, out var result) ? result : throw new ArgumentException($"Invalid userId: {userId}");
                    var puuids = await userGamerRepo.GetGamersPuuidByUserIdAsync(userIdInt);
                    var distinctPuuids = (puuids ?? new List<string>()).Distinct().ToArray();
                    if (distinctPuuids == null 
                    || distinctPuuids.Length == 0)
                    {
                        
                        return Results.Ok(emptyComparisonRequest);
                    }

                    var winrateGamer = "";
            var highestWinrate = -1.0;
            var lowestWinrate = 101.0;

            var kdaGamer = "";
            var highestKda = -1.0;  
            var lowestKda = 101.0;

            var csPrMinGamer = "";
            var highestCsPrMin = -1.0;  
            var lowestCsPrMin = 101.0;

            var goldPrMinGamer = "";
            var highestGoldPrMin = -1.0;  
            var lowestGoldPrMin = 101.0;

            var gamesPlayedGamer = "";
            var highestGamesPlayed = -1;  
            var lowestGamesPlayed = int.MaxValue;

            foreach (var puuid in distinctPuuids)
            {
                
                var gamer = await gamerRepo.GetByPuuidAsync(puuid);
                if (gamer == null)
                {
                    Console.WriteLine($"Gamer with puuid {puuid} not found in database.");
                    continue;
                }

                var winrate = await GetWinrateAsync(matchParticipantRepo, puuid);
                if (winrate > highestWinrate)
                {
                    highestWinrate = winrate;
                    winrateGamer = $"{gamer.GamerName}#{gamer.Tagline}";
                }
                if (winrate < lowestWinrate)
                {
                    lowestWinrate = winrate;
                }         

                var kda = await GetKdaAsync(matchParticipantRepo, puuid);
                if(kda > highestKda)
                {
                    highestKda = kda;
                    kdaGamer = $"{gamer.GamerName}#{gamer.Tagline}";
                }
                if (kda < lowestKda)
                {
                    lowestKda = kda;
                }

                var totalDurationPlayedSeconds = await matchParticipantRepo.GetTotalDurationPlayedByPuuidAsync(puuid);
                var csPrMin = await GetCsPrMinAsync(matchParticipantRepo, puuid, totalDurationPlayedSeconds);
                if(csPrMin > highestCsPrMin)
                {
                    highestCsPrMin = csPrMin;
                    csPrMinGamer = $"{gamer.GamerName}#{gamer.Tagline}";
                }
                if (csPrMin < lowestCsPrMin)
                {
                    lowestCsPrMin = csPrMin;
                }

                var goldPrMin = await GetGoldPrMinAsync(matchParticipantRepo, puuid, totalDurationPlayedSeconds);
                if(goldPrMin > highestGoldPrMin)
                {
                    highestGoldPrMin = goldPrMin;
                    goldPrMinGamer = $"{gamer.GamerName}#{gamer.Tagline}";         
                }
                if (goldPrMin < lowestGoldPrMin)
                {
                    lowestGoldPrMin = goldPrMin;
                }


                var gamesPlayed = await GetGamesPlayedAsync(matchParticipantRepo, puuid);
                if(gamesPlayed > highestGamesPlayed)
                {
                    highestGamesPlayed = gamesPlayed;
                    gamesPlayedGamer = $"{gamer.GamerName}#{gamer.Tagline}";
                }
                if (gamesPlayed < lowestGamesPlayed)
                {
                    lowestGamesPlayed = gamesPlayed;
                }
            }
            var comparisonRequest = new ComparisonRequest(
                Winrate: new Winrate(
                    WinrateDifference: highestWinrate - lowestWinrate,
                    GamerName: winrateGamer
                ),
                Kda: new Kda(
                    KdaDifference: highestKda - lowestKda,
                    GamerName: kdaGamer
                ),
                CsPrMin:  new CsPrMin(
                    CsPrMinDifference: highestCsPrMin - lowestCsPrMin,
                    GamerName: csPrMinGamer
                ),
                GoldPrMin: new GoldPrMin(
                    GoldPrMinDifference: highestGoldPrMin - lowestGoldPrMin,
                    GamerName: goldPrMinGamer
                ),
                GamesPlayed: new GamesPlayed(
                    GamesPlayedDifference: highestGamesPlayed - lowestGamesPlayed,
                    GamerName: gamesPlayedGamer
                )
            );
                    return Results.Ok(comparisonRequest);
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


        private async Task<double> GetWinrateAsync(LolMatchParticipantRepository repo, string puuId)
        {
            var totalMatches = await repo.GetMatchesCountByPuuidAsync(puuId);
            var wins = await repo.GetWinsByPuuidAsync(puuId);
            if (totalMatches == 0)
            {
                return 0;
            }

            return (double)wins / totalMatches * 100;
        }

        private async Task<double> GetKdaAsync(LolMatchParticipantRepository repo, string puuId)
        {
            var totalKills = await repo.GetTotalKillsByPuuidAsync(puuId);
            var totalDeaths = await repo.GetTotalDeathsByPuuidAsync(puuId);
            var totalAssists = await repo.GetTotalAssistsByPuuidAsync(puuId);
            if (totalDeaths == 0)
            {
                return 0;
            }

            return (double)(totalKills + totalAssists) / totalDeaths;
        }

        private async Task<double> GetCsPrMinAsync(LolMatchParticipantRepository repo, string puuId, long totalDurationPlayedSeconds)
        {
            var totalCreepScore = await repo.GetTotalCreepScoreByPuuidAsync(puuId);
            
            if (totalDurationPlayedSeconds == 0)
            {
                return 0;
            }

            var csPrMinValue = (double)totalCreepScore / (totalDurationPlayedSeconds / 60);
            return csPrMinValue;
        }

        private async Task<double> GetGoldPrMinAsync(LolMatchParticipantRepository repo, string puuId, long totalDurationPlayedSeconds)
        {
            var totalGoldEarned = await repo.GetTotalGoldEarnedByPuuidAsync(puuId);
            
            if (totalDurationPlayedSeconds == 0)
            {
                return 0;
            }

            var csPrMinValue = (double)totalGoldEarned / (totalDurationPlayedSeconds / 60);
            return csPrMinValue;
        }

        private async Task<int> GetGamesPlayedAsync(LolMatchParticipantRepository repo, string puuId)
        {
            var totalMatches = await repo.GetMatchesCountByPuuidAsync(puuId);
            return totalMatches;    
        }
    }
}