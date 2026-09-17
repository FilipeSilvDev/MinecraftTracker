using System.ComponentModel;
using System.Runtime.CompilerServices;
using MinecraftTracker.Database;

namespace MinecraftTracker.Models
{
    // Envolve um GameShortcut (persistido no banco) com o status de execução em
    // tempo real, usado pela página "Jogos" para mostrar se cada atalho está aberto.
    public class GameShortcutItem : INotifyPropertyChanged
    {
        public GameShortcut Shortcut { get; }

        public int Id => Shortcut.Id;
        public string Name => Shortcut.Name;
        public string ExecutablePath => Shortcut.ExecutablePath;
        public string ProcessName => Shortcut.ProcessName;

        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (_isRunning != value)
                {
                    _isRunning = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        public string StatusText => IsRunning ? "Em execução" : "Fechado";

        public GameShortcutItem(GameShortcut shortcut)
        {
            Shortcut = shortcut;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
