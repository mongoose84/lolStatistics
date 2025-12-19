using System.Text.Json.Serialization;

namespace RiotProxy.Application.DTOs;

public class OverallStatsDto
{
    public record OverallStatsRequest(
        [property: JsonPropertyName("gamesPlayed")] int GamesPlayed,
        [property: JsonPropertyName("wins")] int Wins
    );
    
}