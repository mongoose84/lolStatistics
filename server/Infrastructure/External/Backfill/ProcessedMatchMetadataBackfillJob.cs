using RiotProxy.Infrastructure.External.Database.Repositories;
using RiotProxy.Infrastructure.External.Riot;
using RiotProxy.External.Domain.Entities;
using System.Text.Json;

namespace RiotProxy.Infrastructure.External.Backfill
{
    /// <summary>
    /// Backfill for matches already marked InfoFetched=true but missing QueueId or GameEndTimestamp/DurationSeconds.
    /// </summary>
    public class ProcessedMatchMetadataBackfillJob : IBackfillJob
    {
        private readonly LolMatchRepository _matchRepository;
        private readonly IRiotApiClient _riotApiClient;
        private IList<LolMatch> _matches = new List<LolMatch>();

        public ProcessedMatchMetadataBackfillJob(LolMatchRepository matchRepository, IRiotApiClient riotApiClient)
        {
            _matchRepository = matchRepository ?? throw new ArgumentNullException(nameof(matchRepository));
            _riotApiClient = riotApiClient ?? throw new ArgumentNullException(nameof(riotApiClient));
        }

        public string Name => "Processed Match Metadata Backfill";

        public async Task<int> GetTotalItemsAsync(CancellationToken ct = default)
        {
            _matches = await _matchRepository.GetProcessedMatchesMissingMetadataAsync();
            return _matches.Count;
        }

        public async Task<int> ProcessBatchAsync(int startIndex, int batchSize, CancellationToken ct = default)
        {
            var batch = _matches.Skip(startIndex).Take(batchSize).ToList();
            var processed = 0;

            foreach (var match in batch)
            {
                if (ct.IsCancellationRequested)
                    break;

                try
                {
                    var matchInfoJson = await _riotApiClient.GetMatchInfoAsync(match.MatchId, ct);

                    var queueId = ExtractQueueId(matchInfoJson);
                    var gameEndTimestamp = ExtractGameEndTimestamp(matchInfoJson);
                    var durationSeconds = ExtractDurationSeconds(matchInfoJson);

                    if (queueId.HasValue || gameEndTimestamp != DateTime.MinValue)
                    {
                        await _matchRepository.UpdateMatchQueueIdTimestampAndDurationAsync(match.MatchId, queueId, gameEndTimestamp, durationSeconds);
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
    }
}
