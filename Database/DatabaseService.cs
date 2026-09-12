using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace TrackerGames
{
    public class GameSession
    {
        public int Id { get; set; }
        public string GameName { get; set; } = "Minecraft Java Edition";
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;

        // Propriedades auxiliares de formatação para a Ui do WinUI3
        public string DateFormatted => StartTime.ToString("dd/MM/yyyy");
        public string StartTimeFormatted => StartTime.ToString("HH:mm");
        public string EndTimeFormatted => EndTime.ToString("HH:mm");
        public string DurationFormatted => Duration.ToString(@"hh\:mm\:ss");
    }

    public class DatabaseService
    {
        private static readonly string DbPath = Path.Combine(AppContext.BaseDirectory, "game_tracker.db");
        private static readonly string ConnectionString = $"Data Source={DbPath}";

        public static async Task InitializeDatabaseAsync()
        {
            using var connection = new SqliteConnection(ConnectionString);
            await connection.OpenAsync();

            var createTableCommand = connection.CreateCommand();
            createTableCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS GameSessions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    GameName TEXT NOT NULL,
                    StartTime TEXT NOT NULL,
                    EndTime TEXT NOT NULL,
                    DurationSeconds INTEGER NOT NULL
                );";
            await createTableCommand.ExecuteNonQueryAsync();
        }

        public static async Task SaveSessionAsync(GameSession session)
        {
            using var connection = new SqliteConnection(ConnectionString);
            await connection.OpenAsync();

            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = @"
                INSERT INTO GameSessions (GameName, StartTime, EndTime, DurationSeconds)
                VALUES ($gameName, $startTime, $endTime, $durationSeconds);";

            insertCommand.Parameters.AddWithValue("$gameName", session.GameName);
            insertCommand.Parameters.AddWithValue("$startTime", session.StartTime.ToString("o")); // Formato ISO 8601
            insertCommand.Parameters.AddWithValue("$endTime", session.EndTime.ToString("o"));
            insertCommand.Parameters.AddWithValue("$durationSeconds", (int)session.Duration.TotalSeconds);

            await insertCommand.ExecuteNonQueryAsync();
        }

        public static async Task<TimeSpan> GetTotalPlayTimeAsync()
        {
            using var connection = new SqliteConnection(ConnectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT SUM(DurationSeconds) FROM GameSessions;";

            var result = await command.ExecuteScalarAsync();
            if (result != DBNull.Value && result != null)
            {
                return TimeSpan.FromSeconds(Convert.ToInt64(result));
            }
            return TimeSpan.Zero;
        }

        public static async Task<TimeSpan> GetTodayPlayTimeAsync()
        {
            using var connection = new SqliteConnection(ConnectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT SUM(DurationSeconds) 
                FROM GameSessions 
                WHERE Date(StartTime) = Date('now', 'localtime');";

            var result = await command.ExecuteScalarAsync();
            if (result != DBNull.Value && result != null)
            {
                return TimeSpan.FromSeconds(Convert.ToInt64(result));
            }
            return TimeSpan.Zero;
        }

        public static async Task<List<GameSession>> GetRecentSessionsAsync(int limit = 20)
        {
            var sessions = new List<GameSession>();

            using var connection = new SqliteConnection(ConnectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, GameName, StartTime, EndTime 
                FROM GameSessions 
                ORDER BY StartTime DESC 
                LIMIT $limit;";
            command.Parameters.AddWithValue("$limit", limit);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                sessions.Add(new GameSession
                {
                    Id = reader.GetInt32(0),
                    GameName = reader.GetString(1),
                    StartTime = DateTime.Parse(reader.GetString(2)),
                    EndTime = DateTime.Parse(reader.GetString(3))
                });
            }

            return sessions;
        }
    }
}