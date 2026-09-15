using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MinecraftTracker.Database;
using MinecraftTracker.Models;

namespace MinecraftTracker.ViewModels
{
    public class GamesViewModel
    {
        public ObservableCollection<GameShortcutItem> Games { get; } = new();

        public GamesViewModel()
        {
            _ = LoadGamesAsync();
            _ = MonitorGamesAsync();
        }

        private async Task LoadGamesAsync()
        {
            var shortcuts = await DatabaseService.GetGameShortcutsAsync();

            Games.Clear();
            foreach (var shortcut in shortcuts)
            {
                Games.Add(new GameShortcutItem(shortcut));
            }
        }

        public async Task AddGameAsync(string name, string executablePath, string processName)
        {
            var shortcut = new GameShortcut
            {
                Name = name,
                ExecutablePath = executablePath,
                ProcessName = processName
            };

            var id = await DatabaseService.AddGameShortcutAsync(shortcut);
            shortcut.Id = id;
            Games.Add(new GameShortcutItem(shortcut));
        }

        public async Task RemoveGameAsync(GameShortcutItem item)
        {
            await DatabaseService.DeleteGameShortcutAsync(item.Id);
            Games.Remove(item);
        }

        // Abre o executável do atalho. A detecção de "em execução" é feita à parte,
        // pelo loop de monitoramento (MonitorGamesAsync), a partir do ProcessName.
        public void LaunchGame(GameShortcutItem item)
        {
            try
            {
                var startInfo = new ProcessStartInfo(item.ExecutablePath)
                {
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(item.ExecutablePath) ?? string.Empty
                };
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao iniciar '{item.Name}': {ex.Message}");
                // TODO: mostrar um ContentDialog de erro para o usuário a partir da GamesPage
            }
        }

        // Verifica periodicamente, para cada atalho cadastrado, se o processo
        // configurado está rodando — mesmo princípio do monitoramento do Minecraft
        // no MainViewModel, só que genérico para qualquer jogo cadastrado aqui.
        private async Task MonitorGamesAsync()
        {
            while (true)
            {
                try
                {
                    var runningProcessNames = Process.GetProcesses()
                        .Select(p =>
                        {
                            try { return p.ProcessName; }
                            catch { return string.Empty; }
                        })
                        .Where(n => !string.IsNullOrEmpty(n))
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    foreach (var game in Games)
                    {
                        game.IsRunning = !string.IsNullOrWhiteSpace(game.ProcessName)
                            && runningProcessNames.Contains(game.ProcessName);
                    }
                }
                catch
                {
                    // Evita que uma falha pontual (ex.: processo fechado durante a leitura) derrube o loop
                }

                await Task.Delay(3000);
            }
        }
    }
}
