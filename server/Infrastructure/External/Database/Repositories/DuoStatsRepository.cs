using MySqlConnector;
using RiotProxy.Infrastructure.External.Database.Records;

namespace RiotProxy.Infrastructure.External.Database.Repositories;

/// <summary>
/// Repository for duo (2-player) game statistics.
/// </summary>
public class DuoStatsRepository
{
    private readonly IDbConnectionFactory _factory;

    public DuoStatsRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Get statistics for games where two players played together (on the same team).
    /// Excludes ARAM games.
    /// </summary>
    public async Task<DuoStatsRecord?> GetDuoStatsByPuuIdsAsync(string puuId1, string puuId2)
    {
        if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
            return null;

        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT
                COUNT(DISTINCT p1.MatchId) as GamesPlayed,
                SUM(CASE WHEN p1.Win = 1 THEN 1 ELSE 0 END) as Wins
            FROM LolMatchParticipant p1
            INNER JOIN LolMatchParticipant p2
                ON p1.MatchId = p2.MatchId
                AND p1.TeamId = p2.TeamId
                AND p1.Puuid != p2.Puuid
            INNER JOIN LolMatch m ON p1.MatchId = m.MatchId
            WHERE p1.Puuid = @puuid1
              AND p2.Puuid = @puuid2
              AND m.InfoFetched = TRUE
              AND m.GameMode != 'ARAM'";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@puuid1", puuId1);
        cmd.Parameters.AddWithValue("@puuid2", puuId2);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new DuoStatsRecord(
                reader.GetInt32("GamesPlayed"),
                reader.GetInt32("Wins"),
                "Excluding ARAM"
            );
        }

        return null;
    }

    /// <summary>
    /// Get side statistics (blue/red) for duo games.
    /// Excludes ARAM games.
    /// </summary>
    public async Task<SideStatsRecord> GetDuoSideStatsByPuuIdsAsync(string puuId1, string puuId2)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT
                SUM(CASE WHEN p1.TeamId = 100 THEN 1 ELSE 0 END) as BlueGames,
                SUM(CASE WHEN p1.TeamId = 100 AND p1.Win = 1 THEN 1 ELSE 0 END) as BlueWins,
                SUM(CASE WHEN p1.TeamId = 200 THEN 1 ELSE 0 END) as RedGames,
                SUM(CASE WHEN p1.TeamId = 200 AND p1.Win = 1 THEN 1 ELSE 0 END) as RedWins
            FROM LolMatchParticipant p1
            INNER JOIN LolMatchParticipant p2
                ON p1.MatchId = p2.MatchId
                AND p1.TeamId = p2.TeamId
                AND p1.Puuid != p2.Puuid
            INNER JOIN LolMatch m ON p1.MatchId = m.MatchId
            WHERE p1.Puuid = @puuid1
              AND p2.Puuid = @puuid2
              AND m.GameMode != 'ARAM'";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@puuid1", puuId1);
        cmd.Parameters.AddWithValue("@puuid2", puuId2);
        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new SideStatsRecord(
                BlueGames: reader.IsDBNull(reader.GetOrdinal("BlueGames")) ? 0 : reader.GetInt32("BlueGames"),
                BlueWins: reader.IsDBNull(reader.GetOrdinal("BlueWins")) ? 0 : reader.GetInt32("BlueWins"),
                RedGames: reader.IsDBNull(reader.GetOrdinal("RedGames")) ? 0 : reader.GetInt32("RedGames"),
                RedWins: reader.IsDBNull(reader.GetOrdinal("RedWins")) ? 0 : reader.GetInt32("RedWins")
            );
        }

        return new SideStatsRecord(0, 0, 0, 0);
    }

    /// <summary>
    /// Get champion synergy statistics for duo games.
    /// Excludes ARAM games.
    /// </summary>
    public async Task<IList<ChampionSynergyRecord>> GetChampionSynergyByPuuIdsAsync(string puuId1, string puuId2)
    {
        if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
            return new List<ChampionSynergyRecord>();

        var records = new List<ChampionSynergyRecord>();
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT
                p1.ChampionId as ChampionId1,
                p1.ChampionName as ChampionName1,
                p2.ChampionId as ChampionId2,
                p2.ChampionName as ChampionName2,
                COUNT(DISTINCT p1.MatchId) as GamesPlayed,
                SUM(CASE WHEN p1.Win = 1 THEN 1 ELSE 0 END) as Wins
            FROM LolMatchParticipant p1
            INNER JOIN LolMatchParticipant p2
                ON p1.MatchId = p2.MatchId
                AND p1.TeamId = p2.TeamId
                AND p1.Puuid != p2.Puuid
            INNER JOIN LolMatch m ON p1.MatchId = m.MatchId
            WHERE p1.Puuid = @puuid1
              AND p2.Puuid = @puuid2
              AND m.InfoFetched = TRUE
              AND m.GameMode != 'ARAM'
            GROUP BY p1.ChampionId, p1.ChampionName, p2.ChampionId, p2.ChampionName
            ORDER BY GamesPlayed DESC";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@puuid1", puuId1);
        cmd.Parameters.AddWithValue("@puuid2", puuId2);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            records.Add(new ChampionSynergyRecord(
                reader.GetInt32("ChampionId1"),
                reader.GetString("ChampionName1"),
                reader.GetInt32("ChampionId2"),
                reader.GetString("ChampionName2"),
                reader.GetInt32("GamesPlayed"),
                reader.GetInt32("Wins")
            ));
        }

        return records;
    }

    /// <summary>
    /// Get multi-kill statistics for duo games.
    /// Excludes ARAM games.
    /// </summary>
    public async Task<IList<DuoMultiKillRecord>> GetDuoMultiKillsAsync(string puuId1, string puuId2)
    {
        if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
            return new List<DuoMultiKillRecord>();

        var records = new List<DuoMultiKillRecord>();
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        foreach (var puuId in new[] { puuId1, puuId2 })
        {
            const string sql = @"
                SELECT
                    SUM(p.DoubleKills) as DoubleKills,
                    SUM(p.TripleKills) as TripleKills,
                    SUM(p.QuadraKills) as QuadraKills,
                    SUM(p.PentaKills) as PentaKills
                FROM LolMatchParticipant p
                INNER JOIN LolMatchParticipant p2 ON p.MatchId = p2.MatchId AND p.TeamId = p2.TeamId AND p.Puuid != p2.Puuid
                INNER JOIN LolMatch m ON p.MatchId = m.MatchId
                WHERE p.Puuid = @puuid
                  AND p2.Puuid = @otherPuuid
                  AND m.InfoFetched = TRUE
                  AND m.DurationSeconds > 0
                  AND m.GameMode != 'ARAM'";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuId);
            cmd.Parameters.AddWithValue("@otherPuuid", puuId == puuId1 ? puuId2 : puuId1);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                records.Add(new DuoMultiKillRecord(
                    PuuId: puuId,
                    DoubleKills: reader.IsDBNull(reader.GetOrdinal("DoubleKills")) ? 0 : reader.GetInt32("DoubleKills"),
                    TripleKills: reader.IsDBNull(reader.GetOrdinal("TripleKills")) ? 0 : reader.GetInt32("TripleKills"),
                    QuadraKills: reader.IsDBNull(reader.GetOrdinal("QuadraKills")) ? 0 : reader.GetInt32("QuadraKills"),
                    PentaKills: reader.IsDBNull(reader.GetOrdinal("PentaKills")) ? 0 : reader.GetInt32("PentaKills")
                ));
            }
        }

        return records;
    }

    /// <summary>
    /// Get win rate trend for duo games.
    /// Excludes ARAM games.
    /// </summary>
    public async Task<IList<DuoWinRateTrendRecord>> GetDuoWinRateTrendAsync(string puuId1, string puuId2, int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
            return new List<DuoWinRateTrendRecord>();

        var records = new List<DuoWinRateTrendRecord>();
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT
                p1.MatchId,
                p1.Win,
                m.GameEndTimestamp as GameDate
            FROM LolMatchParticipant p1
            INNER JOIN LolMatchParticipant p2
                ON p1.MatchId = p2.MatchId
                AND p1.TeamId = p2.TeamId
                AND p1.Puuid != p2.Puuid
            INNER JOIN LolMatch m ON p1.MatchId = m.MatchId
            WHERE p1.Puuid = @puuid1
              AND p2.Puuid = @puuid2
              AND m.InfoFetched = TRUE
              AND m.GameMode != 'ARAM'
            ORDER BY m.GameEndTimestamp DESC
            LIMIT @limit";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@puuid1", puuId1);
        cmd.Parameters.AddWithValue("@puuid2", puuId2);
        cmd.Parameters.AddWithValue("@limit", limit);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            records.Add(new DuoWinRateTrendRecord(
                reader.GetString("MatchId"),
                reader.GetBoolean("Win"),
                reader.IsDBNull(reader.GetOrdinal("GameDate")) ? DateTime.MinValue : reader.GetDateTime("GameDate")
            ));
        }

        records.Reverse(); // Return oldest first
        return records;
    }

    /// <summary>
    /// Get streak data for duo games.
    /// Excludes ARAM games.
    /// </summary>
    public async Task<DuoStreakRecord?> GetDuoStreakAsync(string puuId1, string puuId2)
    {
        if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
            return null;

        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT
                p1.Win
            FROM LolMatchParticipant p1
            INNER JOIN LolMatchParticipant p2
                ON p1.MatchId = p2.MatchId
                AND p1.TeamId = p2.TeamId
                AND p1.Puuid != p2.Puuid
            INNER JOIN LolMatch m ON p1.MatchId = m.MatchId
            WHERE p1.Puuid = @puuid1
              AND p2.Puuid = @puuid2
              AND m.InfoFetched = TRUE
              AND m.GameMode != 'ARAM'
            ORDER BY m.GameEndTimestamp DESC";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@puuid1", puuId1);
        cmd.Parameters.AddWithValue("@puuid2", puuId2);

        await using var reader = await cmd.ExecuteReaderAsync();

        var wins = new List<bool>();
        while (await reader.ReadAsync())
            wins.Add(reader.GetBoolean("Win"));

        if (wins.Count == 0) return null;

        // Calculate current streak
        var currentStreak = 1;
        var isWinStreak = wins[0];
        for (int i = 1; i < wins.Count && wins[i] == isWinStreak; i++)
            currentStreak++;

        // Calculate longest streaks
        var longestWin = 0;
        var longestLoss = 0;
        var streak = 1;
        for (int i = 1; i < wins.Count; i++)
        {
            if (wins[i] == wins[i - 1])
            {
                streak++;
            }
            else
            {
                if (wins[i - 1]) longestWin = Math.Max(longestWin, streak);
                else longestLoss = Math.Max(longestLoss, streak);
                streak = 1;
            }
        }
        if (wins.Count > 0)
        {
            if (wins[^1]) longestWin = Math.Max(longestWin, streak);
            else longestLoss = Math.Max(longestLoss, streak);
        }

        return new DuoStreakRecord(currentStreak, isWinStreak, longestWin, longestLoss);
    }

    /// <summary>
    /// Get the latest game where both players played together on the same team.
    /// </summary>
    public async Task<LatestGameTogetherRecord?> GetLatestGameTogetherByDuoPuuIdsAsync(string puuId1, string puuId2)
    {
        if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
            return null;

        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        // First, find the latest match where both players played together
        const string matchSql = @"
            SELECT p1.MatchId, m.GameEndTimestamp, p1.Win
            FROM LolMatchParticipant p1
            INNER JOIN LolMatchParticipant p2
                ON p1.MatchId = p2.MatchId
                AND p1.TeamId = p2.TeamId
                AND p1.Puuid != p2.Puuid
            INNER JOIN LolMatch m ON p1.MatchId = m.MatchId
            WHERE p1.Puuid = @puuid1
              AND p2.Puuid = @puuid2
              AND m.InfoFetched = TRUE
              AND m.GameEndTimestamp IS NOT NULL
            ORDER BY m.GameEndTimestamp DESC
            LIMIT 1";

        await using var matchCmd = new MySqlCommand(matchSql, conn);
        matchCmd.Parameters.AddWithValue("@puuid1", puuId1);
        matchCmd.Parameters.AddWithValue("@puuid2", puuId2);

        string? matchId = null;
        DateTime gameEndTimestamp = DateTime.MinValue;
        bool win = false;

        await using (var reader = await matchCmd.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                matchId = reader.GetString("MatchId");
                gameEndTimestamp = reader.GetDateTime("GameEndTimestamp");
                win = reader.GetBoolean("Win");
            }
        }

        if (matchId == null)
            return null;

        // Now get each player's details for that match
        const string playersSql = @"
            SELECT
                p.Puuid,
                p.Win,
                COALESCE(NULLIF(p.TeamPosition, ''), 'UNKNOWN') as Role,
                p.ChampionId,
                p.ChampionName,
                p.Kills,
                p.Deaths,
                p.Assists
            FROM LolMatchParticipant p
            WHERE p.MatchId = @matchId
              AND p.Puuid IN (@puuid1, @puuid2)";

        await using var playersCmd = new MySqlCommand(playersSql, conn);
        playersCmd.Parameters.AddWithValue("@matchId", matchId);
        playersCmd.Parameters.AddWithValue("@puuid1", puuId1);
        playersCmd.Parameters.AddWithValue("@puuid2", puuId2);

        var players = new List<LatestGameTogetherPlayerRecord>();
        await using (var reader = await playersCmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                players.Add(new LatestGameTogetherPlayerRecord(
                    Puuid: reader.GetString("Puuid"),
                    Win: reader.GetBoolean("Win"),
                    Role: reader.GetString("Role"),
                    ChampionId: reader.GetInt32("ChampionId"),
                    ChampionName: reader.GetString("ChampionName"),
                    Kills: reader.GetInt32("Kills"),
                    Deaths: reader.GetInt32("Deaths"),
                    Assists: reader.GetInt32("Assists")
                ));
            }
        }

        return new LatestGameTogetherRecord(gameEndTimestamp, win, players);
    }

    /// <summary>
    /// Get performance statistics for duo games (when two players play together).
    /// Excludes ARAM games.
    /// </summary>
    public async Task<PlayerPerformanceRecord?> GetDuoPerformanceByPuuIdsAsync(string puuId1, string puuId2, string targetPuuId)
    {
        if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2) || string.IsNullOrWhiteSpace(targetPuuId))
        {
            return null;
        }

        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        // Get performance stats for targetPuuId in games where both players were on the same team
        const string sql = @"
            SELECT
                COUNT(DISTINCT p1.MatchId) as GamesPlayed,
                SUM(CASE WHEN p1.Win = 1 THEN 1 ELSE 0 END) as Wins,
                SUM(p1.Kills) as TotalKills,
                SUM(p1.Deaths) as TotalDeaths,
                SUM(p1.Assists) as TotalAssists,
                SUM(p1.GoldEarned) as TotalGoldEarned,
                SUM(m.DurationSeconds) as TotalDurationSeconds
            FROM LolMatchParticipant p1
            INNER JOIN LolMatchParticipant p2
                ON p1.MatchId = p2.MatchId
                AND p1.TeamId = p2.TeamId
                AND p1.Puuid != p2.Puuid
            INNER JOIN LolMatch m ON p1.MatchId = m.MatchId
            WHERE p1.Puuid = @targetPuuid
              AND p2.Puuid = @otherPuuid
              AND m.InfoFetched = TRUE
              AND m.DurationSeconds > 0
              AND m.GameMode != 'ARAM'";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@targetPuuid", targetPuuId);
        cmd.Parameters.AddWithValue("@otherPuuid", targetPuuId == puuId1 ? puuId2 : puuId1);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var gamesPlayed = reader.GetInt32("GamesPlayed");
            if (gamesPlayed == 0) return null;

            return new PlayerPerformanceRecord(
                GamesPlayed: gamesPlayed,
                Wins: reader.GetInt32("Wins"),
                TotalKills: reader.GetInt32("TotalKills"),
                TotalDeaths: reader.GetInt32("TotalDeaths"),
                TotalAssists: reader.GetInt32("TotalAssists"),
                TotalGoldEarned: reader.GetInt64("TotalGoldEarned"),
                TotalDurationSeconds: reader.GetInt64("TotalDurationSeconds")
            );
        }

        return null;
    }

    /// <summary>
    /// Get duo champion combination vs enemy champion statistics.
    /// Returns win rates for duo champion combos vs specific enemy lane champions.
    /// </summary>
    public async Task<IList<DuoVsEnemyRecord>> GetDuoVsEnemyByPuuIdsAsync(string puuId1, string puuId2, string? gameMode = null)
    {
        if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
        {
            return new List<DuoVsEnemyRecord>();
        }

        var records = new List<DuoVsEnemyRecord>();
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        // Find duo champion combinations and their performance vs enemy lane champions
        var sql = @"
            SELECT
                p1.ChampionId as DuoChampionId1,
                p1.ChampionName as DuoChampionName1,
                COALESCE(NULLIF(p1.TeamPosition, ''), 'UNKNOWN') as DuoLane1,
                p2.ChampionId as DuoChampionId2,
                p2.ChampionName as DuoChampionName2,
                COALESCE(NULLIF(p2.TeamPosition, ''), 'UNKNOWN') as DuoLane2,
                enemy.TeamPosition as EnemyLane,
                enemy.ChampionId as EnemyChampionId,
                enemy.ChampionName as EnemyChampionName,
                COUNT(DISTINCT p1.MatchId) as GamesPlayed,
                SUM(CASE WHEN p1.Win = 1 THEN 1 ELSE 0 END) as Wins
            FROM LolMatchParticipant p1
            INNER JOIN LolMatchParticipant p2
                ON p1.MatchId = p2.MatchId
                AND p1.TeamId = p2.TeamId
                AND p1.Puuid != p2.Puuid
            INNER JOIN LolMatchParticipant enemy
                ON p1.MatchId = enemy.MatchId
                AND p1.TeamId != enemy.TeamId
            INNER JOIN LolMatch m ON p1.MatchId = m.MatchId
            WHERE p1.Puuid = @puuid1
              AND p2.Puuid = @puuid2
              AND m.InfoFetched = TRUE
              AND enemy.TeamPosition IS NOT NULL
              AND enemy.TeamPosition != ''";

        // Filter by specific game mode if provided, otherwise exclude ARAM
        if (!string.IsNullOrWhiteSpace(gameMode))
        {
            sql += " AND m.GameMode = @gameMode";
        }
        else
        {
            sql += " AND m.GameMode != 'ARAM'";
        }

        sql += @"
            GROUP BY p1.ChampionId, p1.ChampionName, DuoLane1,
                     p2.ChampionId, p2.ChampionName, DuoLane2,
                     enemy.TeamPosition, enemy.ChampionId, enemy.ChampionName
            ORDER BY GamesPlayed DESC";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@puuid1", puuId1);
        cmd.Parameters.AddWithValue("@puuid2", puuId2);

        if (!string.IsNullOrWhiteSpace(gameMode))
        {
            cmd.Parameters.AddWithValue("@gameMode", gameMode);
        }

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            records.Add(new DuoVsEnemyRecord(
                DuoChampionId1: reader.GetInt32("DuoChampionId1"),
                DuoChampionName1: reader.GetString("DuoChampionName1"),
                DuoLane1: reader.GetString("DuoLane1"),
                DuoChampionId2: reader.GetInt32("DuoChampionId2"),
                DuoChampionName2: reader.GetString("DuoChampionName2"),
                DuoLane2: reader.GetString("DuoLane2"),
                EnemyLane: reader.GetString("EnemyLane"),
                EnemyChampionId: reader.GetInt32("EnemyChampionId"),
                EnemyChampionName: reader.GetString("EnemyChampionName"),
                GamesPlayed: reader.GetInt32("GamesPlayed"),
                Wins: reader.GetInt32("Wins")
            ));
        }

        return records;
    }

    /// <summary>
    /// Get role distribution for duo games only (when two players play together).
    /// Returns role distribution for each player in duo games.
    /// </summary>
    public async Task<IList<RoleDistributionRecord>> GetDuoRoleDistributionByPuuIdsAsync(string puuId1, string puuId2, string targetPuuId, string? gameMode = null)
    {
        if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2) || string.IsNullOrWhiteSpace(targetPuuId))
        {
            return new List<RoleDistributionRecord>();
        }

        var records = new List<RoleDistributionRecord>();
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT
                COALESCE(NULLIF(p1.TeamPosition, ''), 'UNKNOWN') as Position,
                COUNT(*) as GamesPlayed
            FROM LolMatchParticipant p1
            INNER JOIN LolMatchParticipant p2
                ON p1.MatchId = p2.MatchId
                AND p1.TeamId = p2.TeamId
                AND p1.Puuid != p2.Puuid
            INNER JOIN LolMatch m ON p1.MatchId = m.MatchId
            WHERE p1.Puuid = @targetPuuid
              AND p2.Puuid = @otherPuuid
              AND m.InfoFetched = TRUE";

        // Filter by specific game mode if provided, otherwise exclude ARAM
        if (!string.IsNullOrWhiteSpace(gameMode))
        {
            sql += " AND m.GameMode = @gameMode";
        }
        else
        {
            sql += " AND m.GameMode != 'ARAM'";
        }

        sql += @"
            GROUP BY Position
            ORDER BY GamesPlayed DESC";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@targetPuuid", targetPuuId);
        cmd.Parameters.AddWithValue("@otherPuuid", puuId1 == targetPuuId ? puuId2 : puuId1);

        if (!string.IsNullOrWhiteSpace(gameMode))
        {
            cmd.Parameters.AddWithValue("@gameMode", gameMode);
        }

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var position = reader.GetString("Position");
            var gamesPlayed = reader.GetInt32("GamesPlayed");

            records.Add(new RoleDistributionRecord(position, gamesPlayed));
        }

        return records;
    }

    /// <summary>
    /// Get lane combination statistics for duo games.
    /// Returns games played and wins for each lane combination.
    /// </summary>
    public async Task<IList<DuoLaneComboRecord>> GetDuoLaneCombosByPuuIdsAsync(string puuId1, string puuId2, string? gameMode = null)
    {
        if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
        {
            return new List<DuoLaneComboRecord>();
        }

        var records = new List<DuoLaneComboRecord>();
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT
                COALESCE(NULLIF(p1.TeamPosition, ''), 'UNKNOWN') as Lane1,
                COALESCE(NULLIF(p2.TeamPosition, ''), 'UNKNOWN') as Lane2,
                COUNT(*) as GamesPlayed,
                SUM(CASE WHEN p1.Win = 1 THEN 1 ELSE 0 END) as Wins
            FROM LolMatchParticipant p1
            INNER JOIN LolMatchParticipant p2
                ON p1.MatchId = p2.MatchId
                AND p1.TeamId = p2.TeamId
                AND p1.Puuid != p2.Puuid
            INNER JOIN LolMatch m ON p1.MatchId = m.MatchId
            WHERE p1.Puuid = @puuid1
              AND p2.Puuid = @puuid2
              AND m.InfoFetched = TRUE";

        // Filter by specific game mode if provided, otherwise exclude ARAM
        if (!string.IsNullOrWhiteSpace(gameMode))
        {
            sql += " AND m.GameMode = @gameMode";
        }
        else
        {
            sql += " AND m.GameMode != 'ARAM'";
        }

        sql += @"
            GROUP BY Lane1, Lane2
            ORDER BY GamesPlayed DESC";;

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@puuid1", puuId1);
        cmd.Parameters.AddWithValue("@puuid2", puuId2);

        if (!string.IsNullOrWhiteSpace(gameMode))
        {
            cmd.Parameters.AddWithValue("@gameMode", gameMode);
        }

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var lane1 = reader.GetString("Lane1");
            var lane2 = reader.GetString("Lane2");
            var gamesPlayed = reader.GetInt32("GamesPlayed");
            var wins = reader.GetInt32("Wins");

            records.Add(new DuoLaneComboRecord(lane1, lane2, gamesPlayed, wins));
        }

        return records;
    }

    /// <summary>
    /// Get kill efficiency statistics for duo games.
    /// Returns kill participation and death share for each player.
    /// </summary>
    public async Task<DuoKillEfficiencyRecord?> GetDuoKillEfficiencyByPuuIdsAsync(string puuId1, string puuId2, string targetPuuId, string? gameMode = null)
    {
        if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2) || string.IsNullOrWhiteSpace(targetPuuId))
        {
            return null;
        }

        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        // Get kill participation (player's kills + assists / team's total kills)
        // and death share in losses (player's deaths / team's total deaths in lost games)
        var sql = @"
            SELECT
                @targetPuuid as PuuId,
                SUM(p.Kills) as TotalKills,
                SUM(p.Assists) as TotalAssists,
                SUM(team_kills.TeamKills) as TeamKills,
                SUM(CASE WHEN p.Win = 0 THEN p.Deaths ELSE 0 END) as DeathsInLosses,
                SUM(CASE WHEN p.Win = 0 THEN team_deaths.TeamDeaths ELSE 0 END) as TeamDeathsInLosses
            FROM LolMatchParticipant p
            INNER JOIN LolMatchParticipant p2
                ON p.MatchId = p2.MatchId
                AND p.TeamId = p2.TeamId
                AND p.Puuid != p2.Puuid
            INNER JOIN LolMatch m ON p.MatchId = m.MatchId
            INNER JOIN (
                SELECT MatchId, TeamId, SUM(Kills) as TeamKills
                FROM LolMatchParticipant
                GROUP BY MatchId, TeamId
            ) team_kills ON p.MatchId = team_kills.MatchId AND p.TeamId = team_kills.TeamId
            INNER JOIN (
                SELECT MatchId, TeamId, SUM(Deaths) as TeamDeaths
                FROM LolMatchParticipant
                GROUP BY MatchId, TeamId
            ) team_deaths ON p.MatchId = team_deaths.MatchId AND p.TeamId = team_deaths.TeamId
            WHERE p.Puuid = @targetPuuid
              AND p2.Puuid = @otherPuuid
              AND m.InfoFetched = TRUE";

        // Filter by specific game mode if provided, otherwise exclude ARAM
        if (!string.IsNullOrWhiteSpace(gameMode))
        {
            sql += " AND m.GameMode = @gameMode";
        }
        else
        {
            sql += " AND m.GameMode != 'ARAM'";
        }

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@targetPuuid", targetPuuId);
        cmd.Parameters.AddWithValue("@otherPuuid", puuId1 == targetPuuId ? puuId2 : puuId1);

        if (!string.IsNullOrWhiteSpace(gameMode))
        {
            cmd.Parameters.AddWithValue("@gameMode", gameMode);
        }

        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var puuId = reader.GetString("PuuId");
            var totalKills = reader.GetInt32("TotalKills");
            var totalAssists = reader.GetInt32("TotalAssists");
            var teamKills = reader.GetInt32("TeamKills");
            var deathsInLosses = reader.GetInt32("DeathsInLosses");
            var teamDeathsInLosses = reader.GetInt32("TeamDeathsInLosses");

            return new DuoKillEfficiencyRecord(puuId, totalKills, totalAssists, teamKills, deathsInLosses, teamDeathsInLosses);
        }

        return null;
    }

    /// <summary>
    /// Get match duration statistics for duo games (when two players play together).
    /// Returns wins/total games grouped by duration buckets.
    /// </summary>
    public async Task<IList<DurationBucketRecord>> GetDuoDurationStatsByPuuIdsAsync(string puuId1, string puuId2, string? gameMode = null)
    {
        if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
        {
            return new List<DurationBucketRecord>();
        }

        var records = new List<DurationBucketRecord>();
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        // Group matches into 5-minute buckets for duo games
        var sql = @"
            SELECT
                FLOOR(m.DurationSeconds / 300) * 5 as MinMinutes,
                COUNT(*) as GamesPlayed,
                SUM(CASE WHEN p1.Win = 1 THEN 1 ELSE 0 END) as Wins
            FROM LolMatchParticipant p1
            INNER JOIN LolMatchParticipant p2
                ON p1.MatchId = p2.MatchId
                AND p1.TeamId = p2.TeamId
                AND p1.Puuid != p2.Puuid
            INNER JOIN LolMatch m ON p1.MatchId = m.MatchId
            WHERE p1.Puuid = @puuid1
              AND p2.Puuid = @puuid2
              AND m.InfoFetched = TRUE
              AND m.DurationSeconds > 0";

        // Filter by specific game mode if provided, otherwise exclude ARAM
        if (!string.IsNullOrWhiteSpace(gameMode))
        {
            sql += " AND m.GameMode = @gameMode";
        }
        else
        {
            sql += " AND m.GameMode != 'ARAM'";
        }

        sql += @"
            GROUP BY MinMinutes
            ORDER BY MinMinutes ASC";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@puuid1", puuId1);
        cmd.Parameters.AddWithValue("@puuid2", puuId2);

        if (!string.IsNullOrWhiteSpace(gameMode))
        {
            cmd.Parameters.AddWithValue("@gameMode", gameMode);
        }

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var minMinutes = reader.GetInt32("MinMinutes");
            var maxMinutes = minMinutes + 5;
            var gamesPlayed = reader.GetInt32("GamesPlayed");
            var wins = reader.GetInt32("Wins");

            records.Add(new DurationBucketRecord(minMinutes, maxMinutes, gamesPlayed, wins));
        }

        return records;
    }

    /// <summary>
    /// Get duo kills grouped by game duration buckets.
    /// </summary>
    public async Task<IList<DuoKillsByDurationRecord>> GetDuoKillsByDurationAsync(string puuId1, string puuId2, string? gameMode = null)
    {
        if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
        {
            return new List<DuoKillsByDurationRecord>();
        }

        var records = new List<DuoKillsByDurationRecord>();
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT
                CASE
                    WHEN m.DurationSeconds < 1200 THEN 'under20'
                    WHEN m.DurationSeconds < 1500 THEN '20-25'
                    WHEN m.DurationSeconds < 1800 THEN '25-30'
                    WHEN m.DurationSeconds < 2100 THEN '30-35'
                    WHEN m.DurationSeconds < 2400 THEN '35-40'
                    ELSE '40+'
                END as DurationBucket,
                COUNT(*) as GamesPlayed,
                SUM(p1.Kills + p2.Kills) as TotalTeamKills,
                SUM(CASE WHEN p1.Win = 1 THEN 1 ELSE 0 END) as Wins
            FROM LolMatchParticipant p1
            INNER JOIN LolMatchParticipant p2 ON p1.MatchId = p2.MatchId AND p1.TeamId = p2.TeamId AND p1.Puuid != p2.Puuid
            INNER JOIN LolMatch m ON p1.MatchId = m.MatchId
            WHERE p1.Puuid = @puuid1
              AND p2.Puuid = @puuid2
              AND m.InfoFetched = TRUE
              AND m.DurationSeconds > 0";

        // Filter by specific game mode if provided, otherwise exclude ARAM
        if (!string.IsNullOrWhiteSpace(gameMode))
        {
            sql += " AND m.GameMode = @gameMode";
        }
        else
        {
            sql += " AND m.GameMode != 'ARAM'";
        }

        sql += " GROUP BY DurationBucket";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@puuid1", puuId1);
        cmd.Parameters.AddWithValue("@puuid2", puuId2);
        if (!string.IsNullOrWhiteSpace(gameMode))
        {
            cmd.Parameters.AddWithValue("@gameMode", gameMode);
        }

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            records.Add(new DuoKillsByDurationRecord(
                DurationBucket: reader.GetString("DurationBucket"),
                GamesPlayed: reader.GetInt32("GamesPlayed"),
                TotalTeamKills: reader.IsDBNull(reader.GetOrdinal("TotalTeamKills")) ? 0 : reader.GetInt32("TotalTeamKills"),
                Wins: reader.GetInt32("Wins")
            ));
        }

        return records;
    }

    /// <summary>
    /// Get kill participation for each player in duo games.
    /// </summary>
    public async Task<IList<DuoKillParticipationRecord>> GetDuoKillParticipationAsync(string puuId1, string puuId2, string? gameMode = null)
    {
        if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
        {
            return new List<DuoKillParticipationRecord>();
        }

        var records = new List<DuoKillParticipationRecord>();
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        foreach (var puuId in new[] { puuId1, puuId2 })
        {
            var sql = @"
                SELECT
                    SUM(p.Kills) as TotalKills,
                    SUM(p.Assists) as TotalAssists,
                    SUM(p.Kills + p2.Kills) as TotalTeamKills,
                    COUNT(*) as GamesPlayed
                FROM LolMatchParticipant p
                INNER JOIN LolMatchParticipant p2 ON p.MatchId = p2.MatchId AND p.TeamId = p2.TeamId AND p.Puuid != p2.Puuid
                INNER JOIN LolMatch m ON p.MatchId = m.MatchId
                WHERE p.Puuid = @puuid
                  AND p2.Puuid = @otherPuuid
                  AND m.InfoFetched = TRUE
                  AND m.DurationSeconds > 0";

            // Filter by specific game mode if provided, otherwise exclude ARAM
            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                sql += " AND m.GameMode = @gameMode";
            }
            else
            {
                sql += " AND m.GameMode != 'ARAM'";
            }

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuId);
            cmd.Parameters.AddWithValue("@otherPuuid", puuId == puuId1 ? puuId2 : puuId1);
            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                cmd.Parameters.AddWithValue("@gameMode", gameMode);
            }

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                records.Add(new DuoKillParticipationRecord(
                    PuuId: puuId,
                    TotalKills: reader.IsDBNull(reader.GetOrdinal("TotalKills")) ? 0 : reader.GetInt32("TotalKills"),
                    TotalAssists: reader.IsDBNull(reader.GetOrdinal("TotalAssists")) ? 0 : reader.GetInt32("TotalAssists"),
                    TotalTeamKills: reader.IsDBNull(reader.GetOrdinal("TotalTeamKills")) ? 0 : reader.GetInt32("TotalTeamKills"),
                    GamesPlayed: reader.GetInt32("GamesPlayed")
                ));
            }
        }

        return records;
    }

    /// <summary>
    /// Get kills trend for duo games (per match data).
    /// </summary>
    public async Task<IList<DuoKillsTrendRecord>> GetDuoKillsTrendAsync(string puuId1, string puuId2, string? gameMode = null, int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
        {
            return new List<DuoKillsTrendRecord>();
        }

        var records = new List<DuoKillsTrendRecord>();
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT
                p1.MatchId,
                (p1.Kills + p2.Kills) as TeamKills,
                p1.Win,
                m.GameEndTimestamp
            FROM LolMatchParticipant p1
            INNER JOIN LolMatchParticipant p2 ON p1.MatchId = p2.MatchId AND p1.TeamId = p2.TeamId AND p1.Puuid != p2.Puuid
            INNER JOIN LolMatch m ON p1.MatchId = m.MatchId
            WHERE p1.Puuid = @puuid1
              AND p2.Puuid = @puuid2
              AND m.InfoFetched = TRUE
              AND m.DurationSeconds > 0";

        // Filter by specific game mode if provided, otherwise exclude ARAM
        if (!string.IsNullOrWhiteSpace(gameMode))
        {
            sql += " AND m.GameMode = @gameMode";
        }
        else
        {
            sql += " AND m.GameMode != 'ARAM'";
        }

        sql += " ORDER BY m.GameEndTimestamp ASC LIMIT @limit";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@puuid1", puuId1);
        cmd.Parameters.AddWithValue("@puuid2", puuId2);
        cmd.Parameters.AddWithValue("@limit", limit);
        if (!string.IsNullOrWhiteSpace(gameMode))
        {
            cmd.Parameters.AddWithValue("@gameMode", gameMode);
        }

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            records.Add(new DuoKillsTrendRecord(
                MatchId: reader.GetString("MatchId"),
                TeamKills: reader.GetInt32("TeamKills"),
                Win: reader.GetBoolean("Win"),
                GameDate: reader.GetDateTime("GameEndTimestamp")
            ));
        }

        return records;
    }

    /// <summary>
    /// Get death timer stats for duo games.
    /// </summary>
    public async Task<IList<DuoDeathTimerRecord>> GetDuoDeathTimerStatsAsync(string puuId1, string puuId2, string? gameMode = null)
    {
        if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
        {
            return new List<DuoDeathTimerRecord>();
        }

        var records = new List<DuoDeathTimerRecord>();
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        foreach (var puuId in new[] { puuId1, puuId2 })
        {
            var sql = @"
                SELECT
                    AVG(CASE WHEN p.Win = 1 THEN p.TimeBeingDeadSeconds ELSE NULL END) as AvgTimeDeadWins,
                    AVG(CASE WHEN p.Win = 0 THEN p.TimeBeingDeadSeconds ELSE NULL END) as AvgTimeDeadLosses,
                    SUM(CASE WHEN p.Win = 1 THEN 1 ELSE 0 END) as WinGames,
                    SUM(CASE WHEN p.Win = 0 THEN 1 ELSE 0 END) as LossGames
                FROM LolMatchParticipant p
                INNER JOIN LolMatchParticipant p2 ON p.MatchId = p2.MatchId AND p.TeamId = p2.TeamId AND p.Puuid != p2.Puuid
                INNER JOIN LolMatch m ON p.MatchId = m.MatchId
                WHERE p.Puuid = @puuid
                  AND p2.Puuid = @otherPuuid
                  AND m.InfoFetched = TRUE
                  AND m.DurationSeconds > 0";

            // Filter by specific game mode if provided, otherwise exclude ARAM
            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                sql += " AND m.GameMode = @gameMode";
            }
            else
            {
                sql += " AND m.GameMode != 'ARAM'";
            }

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuId);
            cmd.Parameters.AddWithValue("@otherPuuid", puuId == puuId1 ? puuId2 : puuId1);
            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                cmd.Parameters.AddWithValue("@gameMode", gameMode);
            }

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                records.Add(new DuoDeathTimerRecord(
                    PuuId: puuId,
                    AvgTimeDeadWins: reader.IsDBNull(reader.GetOrdinal("AvgTimeDeadWins")) ? 0 : reader.GetDouble("AvgTimeDeadWins"),
                    AvgTimeDeadLosses: reader.IsDBNull(reader.GetOrdinal("AvgTimeDeadLosses")) ? 0 : reader.GetDouble("AvgTimeDeadLosses"),
                    WinGames: reader.GetInt32("WinGames"),
                    LossGames: reader.GetInt32("LossGames")
                ));
            }
        }

        return records;
    }

    /// <summary>
    /// Get deaths by duration for duo games.
    /// </summary>
    public async Task<IList<DuoDeathsByDurationRecord>> GetDuoDeathsByDurationAsync(string puuId1, string puuId2, string? gameMode = null)
    {
        if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
        {
            return new List<DuoDeathsByDurationRecord>();
        }

        var records = new List<DuoDeathsByDurationRecord>();
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT
                CASE
                    WHEN m.DurationSeconds < 1200 THEN 'under20'
                    WHEN m.DurationSeconds < 1500 THEN '20-25'
                    WHEN m.DurationSeconds < 1800 THEN '25-30'
                    WHEN m.DurationSeconds < 2100 THEN '30-35'
                    WHEN m.DurationSeconds < 2400 THEN '35-40'
                    ELSE '40+'
                END as DurationBucket,
                COUNT(*) as GamesPlayed,
                SUM(p1.Deaths + p2.Deaths) as TotalTeamDeaths,
                SUM(CASE WHEN p1.Win = 1 THEN 1 ELSE 0 END) as Wins
            FROM LolMatchParticipant p1
            INNER JOIN LolMatchParticipant p2 ON p1.MatchId = p2.MatchId AND p1.TeamId = p2.TeamId AND p1.Puuid != p2.Puuid
            INNER JOIN LolMatch m ON p1.MatchId = m.MatchId
            WHERE p1.Puuid = @puuid1
              AND p2.Puuid = @puuid2
              AND m.InfoFetched = TRUE
              AND m.DurationSeconds > 0";

        // Filter by specific game mode if provided, otherwise exclude ARAM
        if (!string.IsNullOrWhiteSpace(gameMode))
        {
            sql += " AND m.GameMode = @gameMode";
        }
        else
        {
            sql += " AND m.GameMode != 'ARAM'";
        }

        sql += " GROUP BY DurationBucket";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@puuid1", puuId1);
        cmd.Parameters.AddWithValue("@puuid2", puuId2);
        if (!string.IsNullOrWhiteSpace(gameMode))
        {
            cmd.Parameters.AddWithValue("@gameMode", gameMode);
        }

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            records.Add(new DuoDeathsByDurationRecord(
                DurationBucket: reader.GetString("DurationBucket"),
                GamesPlayed: reader.GetInt32("GamesPlayed"),
                TotalTeamDeaths: reader.IsDBNull(reader.GetOrdinal("TotalTeamDeaths")) ? 0 : reader.GetInt32("TotalTeamDeaths"),
                Wins: reader.GetInt32("Wins")
            ));
        }

        return records;
    }

    /// <summary>
    /// Get death share for each player in duo games.
    /// </summary>
    public async Task<IList<DuoDeathShareRecord>> GetDuoDeathShareAsync(string puuId1, string puuId2, string? gameMode = null)
    {
        if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
        {
            return new List<DuoDeathShareRecord>();
        }

        var records = new List<DuoDeathShareRecord>();
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        foreach (var puuId in new[] { puuId1, puuId2 })
        {
            var sql = @"
                SELECT
                    SUM(p.Deaths) as TotalDeaths,
                    COUNT(*) as GamesPlayed
                FROM LolMatchParticipant p
                INNER JOIN LolMatchParticipant p2 ON p.MatchId = p2.MatchId AND p.TeamId = p2.TeamId AND p.Puuid != p2.Puuid
                INNER JOIN LolMatch m ON p.MatchId = m.MatchId
                WHERE p.Puuid = @puuid
                  AND p2.Puuid = @otherPuuid
                  AND m.InfoFetched = TRUE
                  AND m.DurationSeconds > 0";

            // Filter by specific game mode if provided, otherwise exclude ARAM
            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                sql += " AND m.GameMode = @gameMode";
            }
            else
            {
                sql += " AND m.GameMode != 'ARAM'";
            }

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuId);
            cmd.Parameters.AddWithValue("@otherPuuid", puuId == puuId1 ? puuId2 : puuId1);
            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                cmd.Parameters.AddWithValue("@gameMode", gameMode);
            }

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                records.Add(new DuoDeathShareRecord(
                    PuuId: puuId,
                    TotalDeaths: reader.IsDBNull(reader.GetOrdinal("TotalDeaths")) ? 0 : reader.GetInt32("TotalDeaths"),
                    GamesPlayed: reader.GetInt32("GamesPlayed")
                ));
            }
        }

        return records;
    }

    /// <summary>
    /// Get deaths trend for duo games (per match data).
    /// </summary>
    public async Task<IList<DuoDeathsTrendRecord>> GetDuoDeathsTrendAsync(string puuId1, string puuId2, string? gameMode = null, int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
        {
            return new List<DuoDeathsTrendRecord>();
        }

        var records = new List<DuoDeathsTrendRecord>();
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT
                p1.MatchId,
                (p1.Deaths + p2.Deaths) as TeamDeaths,
                p1.Win,
                m.GameEndTimestamp
            FROM LolMatchParticipant p1
            INNER JOIN LolMatchParticipant p2 ON p1.MatchId = p2.MatchId AND p1.TeamId = p2.TeamId AND p1.Puuid != p2.Puuid
            INNER JOIN LolMatch m ON p1.MatchId = m.MatchId
            WHERE p1.Puuid = @puuid1
              AND p2.Puuid = @puuid2
              AND m.InfoFetched = TRUE
              AND m.DurationSeconds > 0";

        // Filter by specific game mode if provided, otherwise exclude ARAM
        if (!string.IsNullOrWhiteSpace(gameMode))
        {
            sql += " AND m.GameMode = @gameMode";
        }
        else
        {
            sql += " AND m.GameMode != 'ARAM'";
        }

        sql += " ORDER BY m.GameEndTimestamp ASC LIMIT @limit";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@puuid1", puuId1);
        cmd.Parameters.AddWithValue("@puuid2", puuId2);
        cmd.Parameters.AddWithValue("@limit", limit);
        if (!string.IsNullOrWhiteSpace(gameMode))
        {
            cmd.Parameters.AddWithValue("@gameMode", gameMode);
        }

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            records.Add(new DuoDeathsTrendRecord(
                MatchId: reader.GetString("MatchId"),
                TeamDeaths: reader.GetInt32("TeamDeaths"),
                Win: reader.GetBoolean("Win"),
                GameDate: reader.GetDateTime("GameEndTimestamp")
            ));
        }

        return records;
    }

    /// <summary>
    /// Get combined performance radar metrics for duo games.
    /// </summary>
    public async Task<DuoPerformanceRadarRecord?> GetDuoPerformanceRadarAsync(string puuId1, string puuId2, string? gameMode = null)
    {
        if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
        {
            return null;
        }

        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT
                AVG((p1.Kills + p2.Kills) / 2.0) as AvgKills,
                AVG((p1.Deaths + p2.Deaths) / 2.0) as AvgDeaths,
                AVG((p1.Assists + p2.Assists) / 2.0) as AvgAssists,
                AVG((p1.CreepScore + p2.CreepScore) / 2.0) as AvgCs,
                AVG((p1.GoldEarned + p2.GoldEarned) / 2.0) as AvgGoldEarned,
                COUNT(*) as GamesPlayed,
                SUM(CASE WHEN p1.Win = 1 THEN 1 ELSE 0 END) as Wins
            FROM LolMatchParticipant p1
            INNER JOIN LolMatchParticipant p2 ON p1.MatchId = p2.MatchId AND p1.TeamId = p2.TeamId AND p1.Puuid != p2.Puuid
            INNER JOIN LolMatch m ON p1.MatchId = m.MatchId
            WHERE p1.Puuid = @puuid1
              AND p2.Puuid = @puuid2
              AND m.InfoFetched = TRUE
              AND m.DurationSeconds > 0";

        // Filter by specific game mode if provided, otherwise exclude ARAM
        if (!string.IsNullOrWhiteSpace(gameMode))
        {
            sql += " AND m.GameMode = @gameMode";
        }
        else
        {
            sql += " AND m.GameMode != 'ARAM'";
        }

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@puuid1", puuId1);
        cmd.Parameters.AddWithValue("@puuid2", puuId2);
        if (!string.IsNullOrWhiteSpace(gameMode))
        {
            cmd.Parameters.AddWithValue("@gameMode", gameMode);
        }

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var gamesPlayed = reader.GetInt32("GamesPlayed");
            if (gamesPlayed == 0)
            {
                return null;
            }

            return new DuoPerformanceRadarRecord(
                AvgKills: reader.IsDBNull(reader.GetOrdinal("AvgKills")) ? 0 : reader.GetDouble("AvgKills"),
                AvgDeaths: reader.IsDBNull(reader.GetOrdinal("AvgDeaths")) ? 0 : reader.GetDouble("AvgDeaths"),
                AvgAssists: reader.IsDBNull(reader.GetOrdinal("AvgAssists")) ? 0 : reader.GetDouble("AvgAssists"),
                AvgCs: reader.IsDBNull(reader.GetOrdinal("AvgCs")) ? 0 : reader.GetDouble("AvgCs"),
                AvgGoldEarned: reader.IsDBNull(reader.GetOrdinal("AvgGoldEarned")) ? 0 : reader.GetDouble("AvgGoldEarned"),
                GamesPlayed: gamesPlayed,
                Wins: reader.GetInt32("Wins")
            );
        }

        return null;
    }
}

