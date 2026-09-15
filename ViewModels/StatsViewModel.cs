using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using MinecraftTracker.Database;
using MinecraftTracker.Models;

namespace MinecraftTracker.ViewModels
{
    public class StatsViewModel : INotifyPropertyChanged
    {
        // Altura máxima (em pixels) que a barra mais alta do gráfico pode ter.
        private const double ChartMaxHeight = 120;

        public ObservableCollection<DayPlaytime> WeeklyChart { get; } = new();

        private string _weekTotal = "0h 0min";
        public string WeekTotal { get => _weekTotal; set { _weekTotal = value; OnPropertyChanged(); } }

        private string _averageSession = "0min";
        public string AverageSession { get => _averageSession; set { _averageSession = value; OnPropertyChanged(); } }

        private string _longestSession = "0min";
        public string LongestSession { get => _longestSession; set { _longestSession = value; OnPropertyChanged(); } }

        private string _sessionsCount = "0";
        public string SessionsCount { get => _sessionsCount; set { _sessionsCount = value; OnPropertyChanged(); } }

        public StatsViewModel()
        {
            RecalculateFromSessions();

            // Recalcula automaticamente sempre que o histórico do dashboard for atualizado
            // (nova sessão detectada, histórico limpo, etc.)
            if (MainViewModel.Current != null)
            {
                MainViewModel.Current.RecentSessions.CollectionChanged += (s, e) => RecalculateFromSessions();
            }
        }

        // NOTA: os cálculos abaixo usam o histórico "recente" já carregado pelo
        // MainViewModel (RecentSessions). Para estatísticas 100% precisas em janelas
        // maiores (ex.: mês inteiro), vale futuramente adicionar um método como
        // DatabaseService.GetSessionsRangeAsync(inicio, fim) e usá-lo aqui.
        private void RecalculateFromSessions()
        {
            var sessions = MainViewModel.Current?.RecentSessions?.ToList() ?? new List<GameSession>();

            if (sessions.Count == 0)
            {
                WeekTotal = "0h 0min";
                AverageSession = "0min";
                LongestSession = "0min";
                SessionsCount = "0";
                BuildWeeklyChart(sessions);
                return;
            }

            var durations = sessions.Select(s => s.EndTime - s.StartTime).ToList();
            var total = TimeSpan.FromTicks(durations.Sum(d => d.Ticks));
            var longest = durations.Max();
            var average = TimeSpan.FromTicks((long)durations.Average(d => d.Ticks));

            WeekTotal = $"{(int)total.TotalHours}h {total.Minutes}min";
            AverageSession = average.TotalHours >= 1
                ? $"{(int)average.TotalHours}h {average.Minutes}min"
                : $"{average.Minutes}min";
            LongestSession = longest.TotalHours >= 1
                ? $"{(int)longest.TotalHours}h {longest.Minutes}min"
                : $"{longest.Minutes}min";
            SessionsCount = sessions.Count.ToString();

            BuildWeeklyChart(sessions);
        }

        private void BuildWeeklyChart(List<GameSession> sessions)
        {
            var culture = new CultureInfo("pt-BR");
            var today = DateTime.Today;
            var minutesByDay = new double[7]; // índice 0 = hoje, 6 = há 6 dias

            foreach (var session in sessions)
            {
                var dayIndex = (today - session.StartTime.Date).Days;
                if (dayIndex >= 0 && dayIndex < 7)
                {
                    minutesByDay[dayIndex] += (session.EndTime - session.StartTime).TotalMinutes;
                }
            }

            double max = minutesByDay.Max();
            if (max <= 0) max = 1; // evita divisão por zero quando não há sessões

            WeeklyChart.Clear();
            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var label = i == 0 ? "Hoje" : culture.DateTimeFormat.GetAbbreviatedDayName(date.DayOfWeek);
                var minutes = minutesByDay[i];

                WeeklyChart.Add(new DayPlaytime
                {
                    DayLabel = label,
                    Minutes = minutes,
                    BarHeight = minutes <= 0 ? 4 : Math.Max(4, (minutes / max) * ChartMaxHeight)
                });
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
