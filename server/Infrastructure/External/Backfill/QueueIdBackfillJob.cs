using RiotProxy.Infrastructure.External.Database.Repositories;
using RiotProxy.Infrastructure.External.Riot;
using RiotProxy.External.Domain.Entities;
using System.Text.Json;

namespace RiotProxy.Infrastructure.External.Backfill
{
    /// <summary>
    /// Backfill job to populate QueueId for existing matches that don't have it.
    /// Also processes unprocessed matches (InfoFetched = FALSE) to fetch and store participant data.
    /// </summary>
    public class QueueIdBackfillJob : IBackfillJob
    {
        private readonly LolMatchRepository _matchRepository;
        private readonly LolMatchParticipantRepository _participantRepository;
        private readonly IRiotApiClient _riotApiClient;
        private IList<LolMatch> _matches = new List<LolMatch>();

        public QueueIdBackfillJob(
            LolMatchRepository matchRepository,
            LolMatchParticipantRepository participantRepository,
            IRiotApiClient riotApiClient)
        {
            _matchRepository = matchRepository ?? throw new ArgumentNullException(nameof(matchRepository));
            _participantRepository = participantRepository ?? throw new ArgumentNullException(nameof(participantRepository));
            _riotApiClient = riotApiClient ?? throw new ArgumentNullException(nameof(riotApiClient));
        }

        public string Name => "QueueId Backfill";

        public async Task<int> GetTotalItemsAsync(CancellationToken ct = default)
        {
            _matches = await _matchRepository.GetMatchesMissingQueueIdAsync();
            return _matches.Count;
        }

        public async Task<int> ProcessBatchAsync(int startIndex, int batchSize, CancellationToken ct = default)
        {
            var batchMatches = _matches
                .Skip(startIndex)
                .Take(batchSize)
                .ToList();

            int processed = 0;

            foreach (var match in batchMatches)
            {
                if (ct.IsCancellationRequested)
                    break;

                try
                {
                    // Fetch match info from Riot API
                    var matchInfoJson = await _riotApiClient.GetMatchInfoAsync(match.MatchId, ct);

                    if (!match.InfoFetched)
                    {
                        // Process unprocessed match: fetch info and add participants
                        ExtractAndMapMatchData(matchInfoJson, match);
                        await _matchRepository.UpdateMatchAsync(match);

                        // Add participants
                        var participants = MapToParticipantEntity(matchInfoJson, match.MatchId);
                        foreach (var participant in participants)
                        {
                            await _participantRepository.AddParticipantIfNotExistsAsync(participant);
                        }
                    }
                    else
                    {
                        // Match already processed: just update missing QueueId/timestamp data
                        var queueId = ExtractQueueId(matchInfoJson);
                        var gameEndTimestamp = ExtractGameEndTimestamp(matchInfoJson);
                        var durationSeconds = ExtractDurationSeconds(matchInfoJson);

                        if (queueId.HasValue || gameEndTimestamp != DateTime.MinValue)
                        {
                            await _matchRepository.UpdateMatchQueueIdTimestampAndDurationAsync(match.MatchId, queueId, gameEndTimestamp, durationSeconds);
                        }
                    }

                    processed++;
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    Console.WriteLine($"[{Name}] Error processing match {match.MatchId}: {ex.Message}");
                }
            }

            return processed;
        }

        public Task OnCompleteAsync(CancellationToken ct = default)
        {
            Console.WriteLine($"[{Name}] Backfill completed successfully.");
            return Task.CompletedTask;
        }

        public Task OnErrorAsync(Exception ex, CancellationToken ct = default)
        {
            Console.WriteLine($"[{Name}] Backfill failed with error: {ex.Message}");
            return Task.CompletedTask;
        }

        private void ExtractAndMapMatchData(JsonDocument matchInfo, LolMatch match)
        {
            if (matchInfo.RootElement.TryGetProperty("info", out var infoElement))
            {
                // gameEndTimestamp is epoch ms; fall back to gameCreation if needed
                var endMs = GetEpochMilliseconds(infoElement, "gameEndTimestamp")
                            ?? GetEpochMilliseconds(infoElement, "gameCreation")
                            ?? 0L;

                if (endMs > 0)
                    match.GameEndTimestamp = DateTimeOffset.FromUnixTimeMilliseconds(endMs).UtcDateTime;
                else
                    match.GameEndTimestamp = DateTime.MinValue;

                match.GameMode = GetGameMode(matchInfo);
                match.QueueId = ExtractQueueId(matchInfo);
                match.InfoFetched = true;
                
                if (infoElement.TryGetProperty("gameDuration", out var gameDurationElement) &&
                    gameDurationElement.ValueKind == JsonValueKind.Number)
                {
                    match.DurationSeconds = gameDurationElement.GetInt64();
                }
            }
        }

        private string GetGameMode(JsonDocument matchInfo)
        {
            if (matchInfo.RootElement.TryGetProperty("info", out var infoElement) &&
                infoElement.TryGetProperty("gameMode", out var gameModeElement))
            {
                if (gameModeElement.ValueKind == JsonValueKind.String)
                    return gameModeElement.GetString() ?? string.Empty;

                return gameModeElement.ToString();
            }
            return string.Empty;
        }

        private int? ExtractQueueId(JsonDocument matchInfo)
        {
            if (matchInfo.RootElement.TryGetProperty("info", out var infoElement) &&
                infoElement.TryGetProperty("queueId", out var queueIdElement))
            {
                if (queueIdElement.ValueKind == JsonValueKind.Number)
                    return queueIdElement.GetInt32();

                if (queueIdElement.ValueKind == JsonValueKind.String &&
                    int.TryParse(queueIdElement.GetString(), out var parsedId))
                    return parsedId;
            }

            return null;
        }

        private DateTime ExtractGameEndTimestamp(JsonDocument matchInfo)
        {
            if (matchInfo.RootElement.TryGetProperty("info", out var infoElement))
            {
                var endMs = GetEpochMilliseconds(infoElement, "gameEndTimestamp")
                            ?? GetEpochMilliseconds(infoElement, "gameCreation")
                            ?? 0L;

                if (endMs > 0)
                    return DateTimeOffset.FromUnixTimeMilliseconds(endMs).UtcDateTime;
            }

            return DateTime.MinValue;
        }

        private long ExtractDurationSeconds(JsonDocument matchInfo)
        {
            if (matchInfo.RootElement.TryGetProperty("info", out var infoElement) &&
                infoElement.TryGetProperty("gameDuration", out var durationElement))
            {
                if (durationElement.ValueKind == JsonValueKind.Number)
                    return durationElement.GetInt64();
            }

            return 0;
        }

        private static long? GetEpochMilliseconds(JsonElement obj, string propertyName)
        {
            if (!obj.TryGetProperty(propertyName, out var el))
                return null;

            return el.ValueKind switch
            {
                JsonValueKind.Number => el.GetInt64(),
                JsonValueKind.String => long.TryParse(el.GetString(), out var v) ? v : null,
                _ => null
            };
        }

        private IList<LolMatchParticipant> MapToParticipantEntity(JsonDocument matchInfo, string matchId)
        {
            var list = new List<LolMatchParticipant>();
            if (!matchInfo.RootElement.TryGetProperty("info", out var infoElement))
                return list;

            if (!infoElement.TryGetProperty("participants", out var participantsElement))
                return list;

            foreach (var participantElement in participantsElement.EnumerateArray())
            {
                var participant = new LolMatchParticipant
                {
                    MatchId = matchId,
                    PuuId = participantElement.TryGetProperty("puuid", out var puuIdEl) ? puuIdEl.GetString() ?? string.Empty : string.Empty,
                    TeamId = participantElement.TryGetProperty("teamId", out var teamIdEl) && teamIdEl.ValueKind == JsonValueKind.Number ? teamIdEl.GetInt32() : 0,
                    Win = participantElement.TryGetProperty("win", out var winEl) && winEl.GetBoolean(),
                    Role = participantElement.TryGetProperty("role", out var roleEl) ? roleEl.GetString() ?? string.Empty : string.Empty,
                    TeamPosition = participantElement.TryGetProperty("teamPosition", out var teamPosEl) ? teamPosEl.GetString() ?? string.Empty : string.Empty,
                    Lane = participantElement.TryGetProperty("lane", out var laneEl) ? laneEl.GetString() ?? string.Empty : string.Empty,
                    ChampionId = participantElement.TryGetProperty("championId", out var champIdEl) && champIdEl.ValueKind == JsonValueKind.Number ? champIdEl.GetInt32() : 0,
                    ChampionName = participantElement.TryGetProperty("championName", out var champEl) ? champEl.GetString() ?? string.Empty : string.Empty,
                    Kills = participantElement.TryGetProperty("kills", out var killsEl) && killsEl.ValueKind == JsonValueKind.Number ? killsEl.GetInt32() : 0,
                    Deaths = participantElement.TryGetProperty("deaths", out var deathsEl) && deathsEl.ValueKind == JsonValueKind.Number ? deathsEl.GetInt32() : 0,
                    Assists = participantElement.TryGetProperty("assists", out var assistsEl) && assistsEl.ValueKind == JsonValueKind.Number ? assistsEl.GetInt32() : 0,
                    DoubleKills = participantElement.TryGetProperty("doubleKills", out var doubleEl) && doubleEl.ValueKind == JsonValueKind.Number ? doubleEl.GetInt32() : 0,
                    TripleKills = participantElement.TryGetProperty("tripleKills", out var tripleEl) && tripleEl.ValueKind == JsonValueKind.Number ? tripleEl.GetInt32() : 0,
                    QuadraKills = participantElement.TryGetProperty("quadraKills", out var quadraEl) && quadraEl.ValueKind == JsonValueKind.Number ? quadraEl.GetInt32() : 0,
                    PentaKills = participantElement.TryGetProperty("pentaKills", out var pentaEl) && pentaEl.ValueKind == JsonValueKind.Number ? pentaEl.GetInt32() : 0,
                    GoldEarned = participantElement.TryGetProperty("goldEarned", out var goldEl) && goldEl.ValueKind == JsonValueKind.Number ? goldEl.GetInt32() : 0,
                    CreepScore = participantElement.TryGetProperty("totalMinionsKilled", out var minionsEl) && minionsEl.ValueKind == JsonValueKind.Number ? minionsEl.GetInt32() : 0,
                    TimeBeingDeadSeconds = participantElement.TryGetProperty("totalTimeDeadSeconds", out var deadEl) && deadEl.ValueKind == JsonValueKind.Number ? deadEl.GetInt32() : 0,
                };

                list.Add(participant);
            }

            return list;
        }
    }
}

