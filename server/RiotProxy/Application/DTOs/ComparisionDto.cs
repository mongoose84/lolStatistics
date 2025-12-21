using System.Text.Json.Serialization;

namespace RiotProxy.Application.DTOs;

public class ComparisionDto
{
    public record ComparisonRequest(
        [property: JsonPropertyName("winrate")] Winrate Winrate,
        [property: JsonPropertyName("kda")] Kda Kda,
        [property: JsonPropertyName("csPrMin")] CsPrMin CsPrMin,
        [property: JsonPropertyName("goldPrMin")] GoldPrMin GoldPrMin,
        [property: JsonPropertyName("gamesPlayed")] GamesPlayed GamesPlayed
     );

    public record Winrate(
        [property: JsonPropertyName("winrateDifference")] double WinrateDifference,
        [property: JsonPropertyName("gamerName")] string GamerName
    );

    public record Kda(
        [property: JsonPropertyName("kdaDifference")] double KdaDifference,
        [property: JsonPropertyName("gamerName")] string GamerName
    );

    public record CsPrMin(
        [property: JsonPropertyName("csPrMinDifference")] double CsPrMinDifference,
        [property: JsonPropertyName("gamerName")] string GamerName
    );

    public record GoldPrMin(
        [property: JsonPropertyName("goldPrMinDifference")] double GoldPrMinDifference,
        [property: JsonPropertyName("gamerName")] string GamerName
    );

    public record GamesPlayed(
        [property: JsonPropertyName("gamesPlayedDifference")] double GamesPlayedDifference,
        [property: JsonPropertyName("gamerName")] string GamerName
    );
            
    
}
