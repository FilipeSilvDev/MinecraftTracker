using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using MinecraftTracker.Database;

namespace MinecraftTracker.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // Instância única compartilhada entre a HomePage e a StatsPage,
        // já que o monitoramento do jogo roda em segundo plano independente da página aberta.
        public static MainViewModel? Current { get; private set; }

        private readonly Stopwatch _gameStopwatch = new(); // Apenas pode ser lida variavel _gameStopWatch
        private DateTime _sessionStartTime; // Hora de início
        private bool _isGameRunning; // Amarzena se jogo esta executando "true or false"
        private string _runningGameTime = "--:--:--"; // Armazena início do timer
        private string _totalPlayTime = "0h 0m"; // Inicio das horas total
        private string _todayPlayTime = "0h 0m"; // Inicio das horas hoje
        public ObservableCollection<GameSession> RecentSessions { get; } = new();
        public string StatusText => IsGameRunning ? "Executando" : "Não detectado";
        public string StatusGlyph => IsGameRunning ? "\uE768" : "\uE769"; // Ícones Fluent Segoe
        public Brush StatusBrush => IsGameRunning
            ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 124, 65))  // #107C41 (Verde)
            : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 138, 136, 134)); // #8A8886 (Cinza)

        public bool IsGameRunning
        {
            get => _isGameRunning;
            set
            {
                if (_isGameRunning != value)
                {
                    _isGameRunning = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(StatusBrush));
                    OnPropertyChanged(nameof(StatusGlyph));
                    _ = HandleGameStatusChanged(_isGameRunning);
                }
            }
        }

        public MainViewModel()
        {
            Current = this;

            // Inicia o monitoramento em segundo plano
            _ = LoadDashboardStatsAsync();
            _ = MonitorGameProcessAsync();
        }

        private async Task MonitorGameProcessAsync()
        {
            while (true)
            {
                CheckIfMinecraftIsRunning();

                if (IsGameRunning)
                {
                    UpdateGameTime();
                    await Task.Delay(1000);
                }
                else
                {
                    await Task.Delay(5000);
                }
            }
        }

        // Wrapper público para permitir que outras páginas (ex.: Configurações, após
        // limpar o histórico) forcem a releitura das estatísticas do banco.
        public async Task ReloadAsync() => await LoadDashboardStatsAsync();

        private async Task LoadDashboardStatsAsync()
        {
            var total = await DatabaseService.GetTotalPlayTimeAsync();
            var today = await DatabaseService.GetTodayPlayTimeAsync();
            var sessions = await DatabaseService.GetRecentSessionsAsync();

            TotalPlayTime = $"{(int)total.TotalHours}h {total.Minutes}min";
            TodayPlayTime = $"{(int)today.TotalHours}h {today.Minutes}min";

            RecentSessions.Clear();
            foreach (var s in sessions)
            {
                RecentSessions.Add(s);
            }
        }

        private void CheckIfMinecraftIsRunning()
        {
            try
            {
                // Busca processos do Java (Java Edition) e do executável nativo do Bedrock
                var processNames = new[] { "javaw", "java", "Minecraft.Windows" };

                var processes = Process.GetProcesses()
                    .Where(p => processNames.Contains(p.ProcessName, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                bool running = false;

                foreach (var proc in processes)
                {
                    try
                    {
                        // Se for Minecraft Bedrock ou tiver "Minecraft" no título da janela
                        if (proc.ProcessName.Equals("Minecraft.Windows", StringComparison.OrdinalIgnoreCase) ||
                            (!string.IsNullOrEmpty(proc.MainWindowTitle) &&
                             proc.MainWindowTitle.Contains("Minecraft", StringComparison.OrdinalIgnoreCase)))
                        {
                            running = true;
                            break;
                        }
                    }
                    catch
                    {
                        // Evita exceção se o processo for fechado durante a verifição
                    }
                }

                // Garante a atualização da UI na Thread principal
                IsGameRunning = running;
            }
            catch
            {
                IsGameRunning = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string RunningGameTime
        {
            get => _runningGameTime;
            set
            { _runningGameTime = value; OnPropertyChanged(); }
        }

        private async Task HandleGameStatusChanged(bool isRunning)
        {
            if (isRunning)
            {
                _sessionStartTime = DateTime.Now;
                _gameStopwatch.Restart();
                UpdateGameTime();
            }
            else
            {
                _gameStopwatch.Stop();

                // Gravar a sessão apenas se ela durar mais de 5 segundos (evita falso alertas)
                if (_gameStopwatch.Elapsed.TotalSeconds >= 30)
                {
                    var session = new GameSession
                    {
                        GameName = "Minecraft Java Edition",
                        StartTime = _sessionStartTime,
                        EndTime = DateTime.Now
                    };

                    await DatabaseService.SaveSessionAsync(session);
                    await LoadDashboardStatsAsync(); // Recarrega estatásticas na UI
                }

                _gameStopwatch.Reset();
                RunningGameTime = "--:--:--";
            }
        }

        private void UpdateGameTime()
        {
            if (_gameStopwatch.IsRunning)
            {
                TimeSpan elapsed = _gameStopwatch.Elapsed;
                RunningGameTime = elapsed.ToString(@"hh\:mm\:ss");
            }
        }

        public string TotalPlayTime
        {
            get => _totalPlayTime;
            set { _totalPlayTime = value; OnPropertyChanged(); }
        }
        public string TodayPlayTime
        {
            get => _todayPlayTime;
            set { _todayPlayTime = value; OnPropertyChanged(); }
        }
    }
}
