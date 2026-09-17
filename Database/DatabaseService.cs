using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Windows.Storage;

namespace MinecraftTracker.Database
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

    // Atalho de jogo cadastrado pelo usuário na aba "Jogos": guarda onde fica o
    // executável e qual processo deve ser observado para saber se o jogo está aberto.
    public class GameShortcut
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;

        // Nome do processo (sem .exe) a monitorar. Pode ser diferente do executável
        // selecionado — por exemplo, o usuário escolhe um launcher, mas quem
        // realmente representa "jogo em execução" é outro processo (ex.: javaw).
        public string ProcessName { get; set; } = string.Empty;
    }

    public class DatabaseService
    {
        private static bool IsPackaged()
        {
            try
            {
                int length = 0; // Tenta recuperar a identidade da aplicacão via API nativa
                return GetCurrentPackageFullName(ref length, null) != 15700; // 15700 = APPMODEL_ERRO_NO_PACKAGE
            }
            catch
            {
                return false;
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, System.Text.StringBuilder? packageFullName);

        private static string GetDatabasePath()
        {
            string folderPath;
            if (IsPackaged())
            {
                folderPath = ApplicationData.Current.LocalFolder.Path;
            }
            else
            {
                folderPath = AppDataPath();
            }

            return Path.Combine(folderPath, "game_tracker.db");
        }

        private static string AppDataPath()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MinecraftTracker");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }

        private static string ConnectionString => $"Data Source={GetDatabasePath()}";

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

            var createShortcutsTableCommand = connection.CreateCommand();
            createShortcutsTableCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS GameShortcuts (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    ExecutablePath TEXT NOT NULL,
                    ProcessName TEXT NOT NULL
                );";
            await createShortcutsTableCommand.ExecuteNonQueryAsync();
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

        // Apaga todo o histórico de sessões (usado pelo botão "Limpar histórico"
        // na Início e nas Configurações). Não afeta os atalhos cadastrados em GameShortcuts.
        public static async Task ClearHistoryAsync()
        {
            using var connection = new SqliteConnection(ConnectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM GameSessions;";
            await command.ExecuteNonQueryAsync();
        }

        // ===== Atalhos de jogos (aba "Jogos") =====

        public static async Task<int> AddGameShortcutAsync(GameShortcut shortcut)
        {
            using var connection = new SqliteConnection(ConnectionString);
            await connection.OpenAsync();

            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = @"
                INSERT INTO GameShortcuts (Name, ExecutablePath, ProcessName)
                VALUES ($name, $executablePath, $processName);
                SELECT last_insert_rowid();";

            insertCommand.Parameters.AddWithValue("$name", shortcut.Name);
            insertCommand.Parameters.AddWithValue("$executablePath", shortcut.ExecutablePath);
            insertCommand.Parameters.AddWithValue("$processName", shortcut.ProcessName);

            var result = await insertCommand.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public static async Task<List<GameShortcut>> GetGameShortcutsAsync()
        {
            var shortcuts = new List<GameShortcut>();

            using var connection = new SqliteConnection(ConnectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Name, ExecutablePath, ProcessName FROM GameShortcuts ORDER BY Name;";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                shortcuts.Add(new GameShortcut
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    ExecutablePath = reader.GetString(2),
                    ProcessName = reader.GetString(3)
                });
            }

            return shortcuts;
        }

        public static async Task DeleteGameShortcutAsync(int id)
        {
            using var connection = new SqliteConnection(ConnectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM GameShortcuts WHERE Id = $id;";
            command.Parameters.AddWithValue("$id", id);

            await command.ExecuteNonQueryAsync();
        }
    }
}
