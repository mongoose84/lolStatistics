using System.Linq;
using FluentAssertions;
using RiotProxy.Application.Services;
using Xunit;

namespace RiotProxy.Tests;

public class MainChampionRecommenderTests
{
    [Fact]
    public void Filters_out_champions_below_minimum_games()
    {
        var stats = new[]
        {
            new MainChampionRecommender.ChampionRoleStats("TOP", 1, "Garen", 1, 1),
            new MainChampionRecommender.ChampionRoleStats("TOP", 2, "Darius", 3, 2)
        };

        var result = MainChampionRecommender.BuildMainChampionsByRole(stats);

        result.Should().HaveCount(1);
        var topRole = result.Single();
        topRole.Role.Should().Be("TOP");
        topRole.Champions.Should().HaveCount(1);
        topRole.Champions[0].ChampionId.Should().Be(2);
    }

    [Fact]
    public void Orders_champions_by_recommended_score()
    {
        var stats = new[]
        {
            // Strong performer: high win rate, decent sample
            new MainChampionRecommender.ChampionRoleStats("JUNGLE", 1, "CarryJg", 10, 9),
            // Weaker performer: mediocre win rate, smaller sample
            new MainChampionRecommender.ChampionRoleStats("JUNGLE", 2, "OtherJg", 4, 2)
        };

        var result = MainChampionRecommender.BuildMainChampionsByRole(stats);

        var jungle = result.Single();
        jungle.Champions.Should().HaveCount(2);
        jungle.Champions[0].ChampionId.Should().Be(1);
    }

    [Fact]
    public void Computes_lp_per_game_from_wins_and_losses()
    {
        var stats = new[]
        {
            new MainChampionRecommender.ChampionRoleStats("MID", 10, "Ahri", 4, 3)
        };

        var result = MainChampionRecommender.BuildMainChampionsByRole(stats);

        var mid = result.Single();
        var champ = mid.Champions.Single();

        var wins = 3;
        var losses = 1;
        const double winLp = 20.0;
        const double lossLp = -15.0;

        var raw = (wins * winLp + losses * lossLp) / (wins + losses);
        var expected = System.Math.Round(raw, 1);

        champ.LpPerGame.Should().BeApproximately(expected, 1e-9);
    }

	    [Fact]
	    public void BuildMainChampionsByRole_IgnoresUnknownRole()
	    {
	        var stats = new[]
	        {
		            new MainChampionRecommender.ChampionRoleStats("UNKNOWN", ChampionId: 1, ChampionName: "SomeChamp", GamesPlayed: 20, Wins: 10),
		            new MainChampionRecommender.ChampionRoleStats("TOP", ChampionId: 2, ChampionName: "Garen", GamesPlayed: 15, Wins: 12)
	        };

	        var result = MainChampionRecommender.BuildMainChampionsByRole(stats);

	        result.Should().HaveCount(1);
	        result[0].Role.Should().Be("TOP");
	    }
}

