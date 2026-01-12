using MySqlConnector;
using RiotProxy.External.Domain.Entities.V2;

namespace RiotProxy.Infrastructure.External.Database.Repositories.V2;

public class V2ParticipantsRepository : RepositoryBase
{
    public V2ParticipantsRepository(IV2DbConnectionFactory factory) : base(factory) {}

    public Task<long> InsertAsync(V2Participant p)
    {
        const string sql = @"INSERT INTO participants
            (match_id, puuid, team_id, role, lane, champion_id, champion_name, win, kills, deaths, assists, creep_score, gold_earned, time_dead_sec, created_at)
            VALUES (@match_id, @puuid, @team_id, @role, @lane, @champion_id, @champion_name, @win, @kills, @deaths, @assists, @creep_score, @gold_earned, @time_dead_sec, @created_at) AS new
            ON DUPLICATE KEY UPDATE
                team_id = new.team_id,
                role = new.role,
                lane = new.lane,
                champion_id = new.champion_id,
                champion_name = new.champion_name,
                win = new.win,
                kills = new.kills,
                deaths = new.deaths,
                assists = new.assists,
                creep_score = new.creep_score,
                gold_earned = new.gold_earned,
                time_dead_sec = new.time_dead_sec;";

        return ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@match_id", p.MatchId);
            cmd.Parameters.AddWithValue("@puuid", p.Puuid);
            cmd.Parameters.AddWithValue("@team_id", p.TeamId);
            cmd.Parameters.AddWithValue("@role", p.Role ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@lane", p.Lane ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@champion_id", p.ChampionId);
            cmd.Parameters.AddWithValue("@champion_name", p.ChampionName);
            cmd.Parameters.AddWithValue("@win", p.Win);
            cmd.Parameters.AddWithValue("@kills", p.Kills);
            cmd.Parameters.AddWithValue("@deaths", p.Deaths);
            cmd.Parameters.AddWithValue("@assists", p.Assists);
            cmd.Parameters.AddWithValue("@creep_score", p.CreepScore);
            cmd.Parameters.AddWithValue("@gold_earned", p.GoldEarned);
            cmd.Parameters.AddWithValue("@time_dead_sec", p.TimeDeadSec);
            cmd.Parameters.AddWithValue("@created_at", p.CreatedAt == default ? DateTime.UtcNow : p.CreatedAt);
            await cmd.ExecuteNonQueryAsync();
            return cmd.LastInsertedId;
        });
    }

    public Task<IList<V2Participant>> GetByMatchAsync(string matchId)
    {
        const string sql = "SELECT * FROM participants WHERE match_id = @match_id";
        return ExecuteListAsync(sql, Map, ("@match_id", matchId));
    }

    public virtual async Task<ISet<string>> GetMatchIdsForPuuidAsync(string puuid)
    {
        const string sql = "SELECT match_id FROM participants WHERE puuid = @puuid";
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuid);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                ids.Add(reader.GetString(0));
            }
            return 0; // dummy return to satisfy signature
        });
        return ids;
    }

    public Task<IList<V2Participant>> GetRecentByPuuidAsync(string puuid, int? queueId, int limit)
    {
        var sql = @"SELECT p.* FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE p.puuid = @puuid";
        if (queueId.HasValue)
        {
            sql += " AND m.queue_id = @queue_id";
        }
        sql += " ORDER BY m.game_start_time DESC LIMIT @limit";

        var parameters = new List<(string, object?)> { ("@puuid", puuid), ("@limit", limit) };
        if (queueId.HasValue)
        {
            parameters.Add(("@queue_id", queueId.Value));
        }
        return ExecuteListAsync(sql, Map, parameters.ToArray());
    }

    /// <summary>
    /// Get champion matchup statistics for multiple puuids.
    /// Returns data grouped by champion+role showing performance against each opponent champion.
    /// Excludes ARAM games (queue_id 450).
    /// </summary>
    public virtual async Task<IList<V2ChampionMatchupRecord>> GetChampionMatchupsByPuuidsAsync(string[] puuids, int? queueId = null)
    {
        if (puuids == null || puuids.Length == 0)
        {
            return new List<V2ChampionMatchupRecord>();
        }

        return await ExecuteWithConnectionAsync(async conn =>
        {
            var records = new List<V2ChampionMatchupRecord>();
            var puuidParams = string.Join(",", puuids.Select((_, i) => $"@puuid{i}"));

            var sql = $@"
                SELECT player.champion_id, player.champion_name, player.role,
                       opponent.champion_id as opponent_champion_id, opponent.champion_name as opponent_champion_name,
                       COUNT(*) as games_played, SUM(CASE WHEN player.win = 1 THEN 1 ELSE 0 END) as wins
                FROM participants player
                INNER JOIN participants opponent
                    ON player.match_id = opponent.match_id
                    AND player.team_id != opponent.team_id
                    AND player.role = opponent.role
                INNER JOIN matches m ON player.match_id = m.match_id
                WHERE player.puuid IN ({puuidParams})
                    AND player.role IS NOT NULL AND player.role != ''
                    AND m.queue_id != 450"; // Exclude ARAM

            if (queueId.HasValue)
            {
                sql += " AND m.queue_id = @queue_id";
            }

            sql += @"
                GROUP BY player.champion_id, player.champion_name, player.role, opponent.champion_id, opponent.champion_name
                ORDER BY player.champion_name, player.role, games_played DESC";

            await using var cmd = new MySqlCommand(sql, conn);
            for (int i = 0; i < puuids.Length; i++)
            {
                cmd.Parameters.AddWithValue($"@puuid{i}", puuids[i]);
            }
            if (queueId.HasValue)
            {
                cmd.Parameters.AddWithValue("@queue_id", queueId.Value);
            }

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                records.Add(new V2ChampionMatchupRecord(
                    reader.GetInt32("champion_id"),
                    reader.GetString("champion_name"),
                    reader.GetString("role"),
                    reader.GetInt32("opponent_champion_id"),
                    reader.GetString("opponent_champion_name"),
                    reader.GetInt32("games_played"),
                    reader.GetInt32("wins")
                ));
            }
            return records;
        });
    }

    private static V2Participant Map(MySqlDataReader r) => new()
    {
        Id = r.GetInt64(0),
        MatchId = r.GetString(1),
        Puuid = r.GetString(2),
        TeamId = r.GetInt32(3),
        Role = r.IsDBNull(4) ? null : r.GetString(4),
        Lane = r.IsDBNull(5) ? null : r.GetString(5),
        ChampionId = r.GetInt32(6),
        ChampionName = r.GetString(7),
        Win = r.GetBoolean(8),
        Kills = r.GetInt32(9),
        Deaths = r.GetInt32(10),
        Assists = r.GetInt32(11),
        CreepScore = r.GetInt32(12),
        GoldEarned = r.GetInt32(13),
        TimeDeadSec = r.GetInt32(14),
        CreatedAt = r.GetDateTime(15)
    };
}

/// <summary>
/// Record representing champion matchup statistics for v2.
/// </summary>
public record V2ChampionMatchupRecord(
    int ChampionId,
    string ChampionName,
    string Role,
    int OpponentChampionId,
    string OpponentChampionName,
    int GamesPlayed,
    int Wins
);
