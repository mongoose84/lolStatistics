using MySqlConnector;
using RiotProxy.External.Domain.Entities;
using RiotProxy.Infrastructure.External.Database.Records;

namespace RiotProxy.Infrastructure.External.Database.Repositories
{
    public class LolMatchParticipantRepository
    {
        private readonly IDbConnectionFactory _factory;

        public LolMatchParticipantRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task AddParticipantIfNotExistsAsync(LolMatchParticipant participant)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            const string sql = "INSERT IGNORE INTO LolMatchParticipant (MatchId, Puuid, TeamId, Win, Role, TeamPosition, Lane, ChampionId, ChampionName, Kills, Deaths, Assists, DoubleKills, TripleKills, QuadraKills, PentaKills, GoldEarned, TimeBeingDeadSeconds, CreepScore) " +
                               "VALUES (@matchId, @puuid, @teamId, @win, @role, @teamPosition, @lane, @championId, @championName, @kills, @deaths, @assists, @doubleKills, @tripleKills, @quadraKills, @pentaKills, @goldEarned, @timeBeingDeadSeconds, @creepScore)";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@matchId", participant.MatchId);
            cmd.Parameters.AddWithValue("@puuid", participant.PuuId);
            cmd.Parameters.AddWithValue("@teamId", participant.TeamId);
            cmd.Parameters.AddWithValue("@win", participant.Win);
            cmd.Parameters.AddWithValue("@role", participant.Role ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@teamPosition", participant.TeamPosition ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@lane", participant.Lane ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@championId", participant.ChampionId);
            cmd.Parameters.AddWithValue("@championName", participant.ChampionName);
            cmd.Parameters.AddWithValue("@kills", participant.Kills);
            cmd.Parameters.AddWithValue("@deaths", participant.Deaths);
            cmd.Parameters.AddWithValue("@assists", participant.Assists);
            cmd.Parameters.AddWithValue("@doubleKills", participant.DoubleKills);
            cmd.Parameters.AddWithValue("@tripleKills", participant.TripleKills);
            cmd.Parameters.AddWithValue("@quadraKills", participant.QuadraKills);
            cmd.Parameters.AddWithValue("@pentaKills", participant.PentaKills);
            cmd.Parameters.AddWithValue("@goldEarned", participant.GoldEarned);
            cmd.Parameters.AddWithValue("@timeBeingDeadSeconds", participant.TimeBeingDeadSeconds);
            cmd.Parameters.AddWithValue("@creepScore", participant.CreepScore);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<IList<string>> GetMatchIdsForPuuidAsync(string puuId)
        {
            var matchIds = new List<string>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            const string sql = "SELECT MatchId FROM LolMatchParticipant WHERE Puuid = @puuid";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuId);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                matchIds.Add(reader.GetString(0));
            }
            return matchIds;
        }

        internal async Task<long> GetTotalDurationPlayedByPuuidAsync(string puuId)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            const string sql = "SELECT SUM(DurationSeconds) FROM LolMatch WHERE MatchId IN (SELECT MatchId FROM LolMatchParticipant WHERE Puuid = @puuid)";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuId);
            var result = await cmd.ExecuteScalarAsync();
            return result != DBNull.Value ? Convert.ToInt64(result) : 0L;
        }

        /// <summary>
        /// Get total duration played excluding ARAM games (for CS/min and Gold/min calculations)
        /// </summary>
        internal async Task<long> GetTotalDurationPlayedExcludingAramByPuuidAsync(string puuId)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT COALESCE(SUM(m.DurationSeconds), 0)
                FROM LolMatch m
                INNER JOIN LolMatchParticipant p ON m.MatchId = p.MatchId
                WHERE p.Puuid = @puuid AND m.GameMode != 'ARAM'";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuId);
            var result = await cmd.ExecuteScalarAsync();
            return result != DBNull.Value ? Convert.ToInt64(result) : 0L;
        }

        /// <summary>
        /// Get total creep score excluding ARAM games (for CS/min calculations)
        /// </summary>
        internal async Task<int> GetTotalCreepScoreExcludingAramByPuuIdAsync(string puuId)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT COALESCE(SUM(p.CreepScore), 0)
                FROM LolMatchParticipant p
                INNER JOIN LolMatch m ON p.MatchId = m.MatchId
                WHERE p.Puuid = @puuid AND m.GameMode != 'ARAM'";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuId);
            var result = await cmd.ExecuteScalarAsync();
            return result != DBNull.Value ? Convert.ToInt32(result) : 0;
        }

        /// <summary>
        /// Get total gold earned excluding ARAM games (for Gold/min calculations)
        /// </summary>
        internal async Task<int> GetTotalGoldEarnedExcludingAramByPuuIdAsync(string puuId)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT COALESCE(SUM(p.GoldEarned), 0)
                FROM LolMatchParticipant p
                INNER JOIN LolMatch m ON p.MatchId = m.MatchId
                WHERE p.Puuid = @puuid AND m.GameMode != 'ARAM'";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuId);
            var result = await cmd.ExecuteScalarAsync();
            return result != DBNull.Value ? Convert.ToInt32(result) : 0;
        }

        internal async Task<int> GetWinsByPuuIdAsync(string puuId)
        {
            const string sql = "SELECT COUNT(*) FROM LolMatchParticipant WHERE Puuid = @puuid AND Win = TRUE";
            var wins = await GetIntegerValueFromPuuIdAsync(puuId, sql);
            return wins;
        }

        internal async Task<int> GetMatchesCountByPuuIdAsync(string puuId)
        {
            const string sql = "SELECT COUNT(*) FROM LolMatchParticipant WHERE Puuid = @puuid";
            var totalMatches = await GetIntegerValueFromPuuIdAsync(puuId, sql);
            return totalMatches;
        }

        internal async Task<int> GetTotalAssistsByPuuIdAsync(string puuId)
        {
            const string sql = "SELECT SUM(Assists) FROM LolMatchParticipant WHERE Puuid = @puuid";
            var totalAssists = await GetIntegerValueFromPuuIdAsync(puuId, sql);
            return totalAssists;
        }

        internal async Task<int> GetTotalDeathsByPuuIdAsync(string puuId)
        {
            const string sql = "SELECT SUM(Deaths) FROM LolMatchParticipant WHERE Puuid = @puuid";
            var totalDeaths = await GetIntegerValueFromPuuIdAsync(puuId, sql);
            return totalDeaths;
        }

        internal async Task<int> GetTotalKillsByPuuIdAsync(string puuId)
        {
            const string sql = "SELECT SUM(Kills) FROM LolMatchParticipant WHERE Puuid = @puuid";
            var totalKills = await GetIntegerValueFromPuuIdAsync(puuId, sql);
            return totalKills;
        }

        internal async Task<int> GetTotalCreepScoreByPuuIdAsync(string puuId)
        {
            const string sql = "SELECT SUM(CreepScore) FROM LolMatchParticipant WHERE Puuid = @puuid";
            var totalCreepScore = await GetIntegerValueFromPuuIdAsync(puuId, sql);
            return totalCreepScore;
        }

        internal async Task<int> GetTotalGoldEarnedByPuuIdAsync(string puuId)
        {
            const string sql = "SELECT SUM(GoldEarned) FROM LolMatchParticipant WHERE Puuid = @puuid";
            var totalGoldEarned = await GetIntegerValueFromPuuIdAsync(puuId, sql);
            return totalGoldEarned;
        }

        internal async Task<int> GetTotalTimeBeingDeadSecondsByPuuIdAsync(string puuId)
        {
            const string sql = "SELECT SUM(TimeBeingDeadSeconds) FROM LolMatchParticipant WHERE Puuid = @puuid";
            var totalTimeBeingDeadSeconds = await GetIntegerValueFromPuuIdAsync(puuId, sql);
            return totalTimeBeingDeadSeconds;
        }

        /// <summary>
        /// Gets the timestamp of the most recent game played by a specific player.
        /// </summary>
        internal async Task<DateTime?> GetLatestGameTimestampByPuuIdAsync(string puuId)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT MAX(m.GameEndTimestamp)
                FROM LolMatchParticipant p
                INNER JOIN LolMatch m ON p.MatchId = m.MatchId
                WHERE p.Puuid = @puuid
                  AND m.InfoFetched = TRUE
                  AND m.GameEndTimestamp IS NOT NULL";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuId);
            var result = await cmd.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value)
                return null;

            return Convert.ToDateTime(result);
        }

        /// <summary>
        /// Gets detailed information about the most recent game played by a specific player.
        /// </summary>
        internal async Task<LatestGameRecord?> GetLatestGameDetailsByPuuIdAsync(string puuId)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT
                    m.GameEndTimestamp,
                    p.Win,
                    COALESCE(NULLIF(p.TeamPosition, ''), 'UNKNOWN') as Role,
                    p.ChampionId,
                    p.ChampionName,
                    p.Kills,
                    p.Deaths,
                    p.Assists
                FROM LolMatchParticipant p
                INNER JOIN LolMatch m ON p.MatchId = m.MatchId
                WHERE p.Puuid = @puuid
                  AND m.InfoFetched = TRUE
                  AND m.GameEndTimestamp IS NOT NULL
                ORDER BY m.GameEndTimestamp DESC
                LIMIT 1";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuId);
            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new LatestGameRecord(
                    GameEndTimestamp: reader.GetDateTime("GameEndTimestamp"),
                    Win: reader.GetBoolean("Win"),
                    Role: reader.GetString("Role"),
                    ChampionId: reader.GetInt32("ChampionId"),
                    ChampionName: reader.GetString("ChampionName"),
                    Kills: reader.GetInt32("Kills"),
                    Deaths: reader.GetInt32("Deaths"),
                    Assists: reader.GetInt32("Assists")
                );
            }

            return null;
        }

        private async Task<int> GetIntegerValueFromPuuIdAsync(string puuId, string sqlQuery)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(sqlQuery, conn);
            cmd.Parameters.AddWithValue("@puuid", puuId);
            var result = await cmd.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value)
                return 0;
            return Convert.ToInt32(result);
        }

        /// <summary>
        /// Gets per-match performance data for a player, ordered by game end time.
        /// Used for performance timeline charts.
        /// </summary>
        /// <param name="puuId">Player's PUUID</param>
        /// <param name="fromDate">Start date filter (null for all time)</param>
        /// <param name="limit">Maximum number of matches to return (null for no limit). Returns the most recent matches.</param>
        /// <returns>List of match performance records ordered oldest to newest</returns>
        public async Task<IList<MatchPerformanceRecord>> GetMatchPerformanceTimelineAsync(string puuId, DateTime? fromDate = null, int? limit = null)
        {
            var records = new List<MatchPerformanceRecord>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            // If limit is specified, we need to get the most recent N matches first, then reverse them
            // We'll use a subquery to get the latest N matches, then order them chronologically
            string sql;

            if (limit.HasValue)
            {
                // Get the latest N matches ordered chronologically (oldest to newest)
                // Using a subquery to first get the most recent N matches, then order them chronologically
                // Excludes ARAM games since CS/min and Gold/min are not meaningful for ARAM
                sql = @"
                    SELECT * FROM (
                        SELECT
                            p.Win,
                            p.GoldEarned,
                            p.CreepScore,
                            m.DurationSeconds,
                            m.GameEndTimestamp
                        FROM LolMatchParticipant p
                        INNER JOIN LolMatch m ON p.MatchId = m.MatchId
                        WHERE p.Puuid = @puuid
                          AND m.InfoFetched = TRUE
                          AND m.DurationSeconds > 0
                          AND m.GameMode != 'ARAM'";

                if (fromDate.HasValue)
                {
                    sql += " AND m.GameEndTimestamp >= @fromDate";
                }

                sql += @"
                        ORDER BY m.GameEndTimestamp DESC
                        LIMIT @limit
                    ) AS recent_matches
                    ORDER BY GameEndTimestamp ASC";
            }
            else
            {
                // No limit - get all matches
                // Excludes ARAM games since CS/min and Gold/min are not meaningful for ARAM
                sql = @"
                    SELECT
                        p.Win,
                        p.GoldEarned,
                        p.CreepScore,
                        m.DurationSeconds,
                        m.GameEndTimestamp
                    FROM LolMatchParticipant p
                    INNER JOIN LolMatch m ON p.MatchId = m.MatchId
                    WHERE p.Puuid = @puuid
                      AND m.InfoFetched = TRUE
                      AND m.DurationSeconds > 0
                      AND m.GameMode != 'ARAM'";

                if (fromDate.HasValue)
                {
                    sql += " AND m.GameEndTimestamp >= @fromDate";
                }

                sql += " ORDER BY m.GameEndTimestamp ASC";
            }

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuId);
            if (fromDate.HasValue)
            {
                cmd.Parameters.AddWithValue("@fromDate", fromDate.Value);
            }
            if (limit.HasValue)
            {
                cmd.Parameters.AddWithValue("@limit", limit.Value);
            }

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var durationSeconds = reader.GetInt64(3);
                var durationMinutes = durationSeconds / 60.0;

                records.Add(new MatchPerformanceRecord(
                    Win: reader.GetBoolean(0),
                    GoldEarned: reader.GetInt32(1),
                    CreepScore: reader.GetInt32(2),
                    DurationMinutes: durationMinutes,
                    GameEndTimestamp: reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4)
                ));
            }

            // No need to reverse - the SQL query now handles ordering correctly
            // Both limited and unlimited queries return results in chronological order (oldest to newest)
            return records;
        }

        /// <summary>
        /// Get champion statistics (games played, wins) grouped by champion for a specific puuid.
        /// Excludes ARAM games.
        /// </summary>
        internal async Task<IList<ChampionStatsRecord>> GetChampionStatsByPuuIdAsync(string puuId)
        {
            var records = new List<ChampionStatsRecord>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT
                    p.ChampionId,
                    p.ChampionName,
                    COUNT(*) as GamesPlayed,
                    SUM(CASE WHEN p.Win = 1 THEN 1 ELSE 0 END) as Wins
                FROM LolMatchParticipant p
                INNER JOIN LolMatch m ON p.MatchId = m.MatchId
                WHERE p.Puuid = @puuid
                  AND m.GameMode != 'ARAM'
                GROUP BY p.ChampionId, p.ChampionName
                ORDER BY GamesPlayed DESC";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuId);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var championId = reader.GetInt32("ChampionId");
                var championName = reader.GetString("ChampionName");
                var gamesPlayed = reader.GetInt32("GamesPlayed");
                var wins = reader.GetInt32("Wins");

                records.Add(new ChampionStatsRecord(championId, championName, gamesPlayed, wins));
            }

            return records;
        }

        /// <summary>
        /// Get role/position distribution for a specific puuid.
        /// Returns count of games played in each role/position.
        /// Excludes ARAM games.
        /// </summary>
        internal async Task<IList<RoleDistributionRecord>> GetRoleDistributionByPuuIdAsync(string puuId)
        {
            var records = new List<RoleDistributionRecord>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            // Use TeamPosition as it's more reliable than Role or Lane in modern League
            const string sql = @"
                SELECT
                    COALESCE(NULLIF(p.TeamPosition, ''), 'UNKNOWN') as Position,
                    COUNT(*) as GamesPlayed
                FROM LolMatchParticipant p
                INNER JOIN LolMatch m ON p.MatchId = m.MatchId
                WHERE p.Puuid = @puuid
                  AND m.GameMode != 'ARAM'
                GROUP BY Position
                ORDER BY GamesPlayed DESC";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuId);
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
        /// Get side statistics (blue/red) for a specific puuid.
        /// TeamId 100 = Blue side, TeamId 200 = Red side.
        /// Excludes ARAM games.
        /// </summary>
        internal async Task<SideStatsRecord> GetSideStatsByPuuIdAsync(string puuId)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT
                    SUM(CASE WHEN p.TeamId = 100 THEN 1 ELSE 0 END) as BlueGames,
                    SUM(CASE WHEN p.TeamId = 100 AND p.Win = 1 THEN 1 ELSE 0 END) as BlueWins,
                    SUM(CASE WHEN p.TeamId = 200 THEN 1 ELSE 0 END) as RedGames,
                    SUM(CASE WHEN p.TeamId = 200 AND p.Win = 1 THEN 1 ELSE 0 END) as RedWins
                FROM LolMatchParticipant p
                INNER JOIN LolMatch m ON p.MatchId = m.MatchId
                WHERE p.Puuid = @puuid
                  AND m.GameMode != 'ARAM'";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuId);
            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new SideStatsRecord(
                    BlueGames: reader.GetInt32("BlueGames"),
                    BlueWins: reader.GetInt32("BlueWins"),
                    RedGames: reader.GetInt32("RedGames"),
                    RedWins: reader.GetInt32("RedWins")
                );
            }

            return new SideStatsRecord(0, 0, 0, 0);
        }

        /// <summary>
        /// Get match duration statistics (wins/total games) grouped by duration buckets for a specific puuid.
        /// Excludes ARAM games.
        /// </summary>
        internal async Task<IList<DurationBucketRecord>> GetDurationStatsByPuuIdAsync(string puuId)
        {
            var records = new List<DurationBucketRecord>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            // Group matches into 5-minute buckets
            const string sql = @"
                SELECT
                    FLOOR(m.DurationSeconds / 300) * 5 as MinMinutes,
                    COUNT(*) as GamesPlayed,
                    SUM(CASE WHEN p.Win = 1 THEN 1 ELSE 0 END) as Wins
                FROM LolMatchParticipant p
                INNER JOIN LolMatch m ON p.MatchId = m.MatchId
                WHERE p.Puuid = @puuid
                  AND m.InfoFetched = TRUE
                  AND m.DurationSeconds > 0
                  AND m.GameMode != 'ARAM'
                GROUP BY MinMinutes
                ORDER BY MinMinutes ASC";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuId);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var minMinutes = reader.GetInt32("MinMinutes");
                var gamesPlayed = reader.GetInt32("GamesPlayed");
                var wins = reader.GetInt32("Wins");

                records.Add(new DurationBucketRecord(minMinutes, minMinutes + 5, gamesPlayed, wins));
            }

            return records;
        }

        /// <summary>
        /// Get champion matchup statistics for multiple puuids.
        /// Returns data grouped by champion+role showing performance against each opponent champion.
        /// </summary>
        internal async Task<IList<ChampionMatchupRecord>> GetChampionMatchupsByPuuIdsAsync(string[] puuIds)
        {
            if (puuIds == null || puuIds.Length == 0)
            {
                return new List<ChampionMatchupRecord>();
            }

            var records = new List<ChampionMatchupRecord>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            // Build parameterized query for multiple puuids
            var puuidParams = string.Join(",", puuIds.Select((_, i) => $"@puuid{i}"));

            // This query finds all matches where the player played a champion in a role,
            // then joins to find the opponent in the same role on the enemy team
            // Filters out UNKNOWN roles (empty or null TeamPosition)
            // Excludes ARAM games
            var sql = $@"
                SELECT
                    player.ChampionId,
                    player.ChampionName,
                    player.TeamPosition as Role,
                    opponent.ChampionId as OpponentChampionId,
                    opponent.ChampionName as OpponentChampionName,
                    COUNT(*) as GamesPlayed,
                    SUM(CASE WHEN player.Win = 1 THEN 1 ELSE 0 END) as Wins
                FROM LolMatchParticipant player
                INNER JOIN LolMatchParticipant opponent
                    ON player.MatchId = opponent.MatchId
                    AND player.TeamId != opponent.TeamId
                    AND player.TeamPosition = opponent.TeamPosition
                INNER JOIN LolMatch m ON player.MatchId = m.MatchId
                WHERE player.Puuid IN ({puuidParams})
                    AND player.TeamPosition IS NOT NULL
                    AND player.TeamPosition != ''
                    AND m.GameMode != 'ARAM'
                GROUP BY player.ChampionId, player.ChampionName, Role, opponent.ChampionId, opponent.ChampionName
                ORDER BY player.ChampionName, Role, GamesPlayed DESC";

            await using var cmd = new MySqlCommand(sql, conn);
            for (int i = 0; i < puuIds.Length; i++)
            {
                cmd.Parameters.AddWithValue($"@puuid{i}", puuIds[i]);
            }

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var championId = reader.GetInt32("ChampionId");
                var championName = reader.GetString("ChampionName");
                var role = reader.GetString("Role");
                var opponentChampionId = reader.GetInt32("OpponentChampionId");
                var opponentChampionName = reader.GetString("OpponentChampionName");
                var gamesPlayed = reader.GetInt32("GamesPlayed");
                var wins = reader.GetInt32("Wins");

                records.Add(new ChampionMatchupRecord(
                    championId,
                    championName,
                    role,
                    opponentChampionId,
                    opponentChampionName,
                    gamesPlayed,
                    wins
                ));
            }

            return records;
        }

        /// <summary>
        /// Get performance statistics for solo games (when a player plays without a specific partner).
        /// Excludes ARAM games when gameMode is not specified.
        /// </summary>
        internal async Task<PlayerPerformanceRecord?> GetSoloPerformanceByPuuIdAsync(string puuId, string excludePuuId, string? gameMode = null)
        {
            if (string.IsNullOrWhiteSpace(puuId))
            {
                return null;
            }

            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            // Get performance stats for puuId in games where excludePuuId was NOT on the same team
            var sql = @"
                SELECT
                    COUNT(DISTINCT p.MatchId) as GamesPlayed,
                    SUM(CASE WHEN p.Win = 1 THEN 1 ELSE 0 END) as Wins,
                    SUM(p.Kills) as TotalKills,
                    SUM(p.Deaths) as TotalDeaths,
                    SUM(p.Assists) as TotalAssists,
                    SUM(p.GoldEarned) as TotalGoldEarned,
                    SUM(m.DurationSeconds) as TotalDurationSeconds
                FROM LolMatchParticipant p
                INNER JOIN LolMatch m ON p.MatchId = m.MatchId
                WHERE p.Puuid = @puuid
                  AND m.InfoFetched = TRUE
                  AND m.DurationSeconds > 0
                  AND NOT EXISTS (
                      SELECT 1 FROM LolMatchParticipant p2
                      WHERE p2.MatchId = p.MatchId
                        AND p2.TeamId = p.TeamId
                        AND p2.Puuid = @excludePuuid
                  )";

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
            cmd.Parameters.AddWithValue("@excludePuuid", excludePuuId);

            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                cmd.Parameters.AddWithValue("@gameMode", gameMode);
            }

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
    }
}
