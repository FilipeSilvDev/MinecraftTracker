using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MinecraftTrackerApp;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MinecraftTracker
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();

            // Instância o banco de dados
            try
            {
                await DatabaseService.InitializeDatabaseAsync();
            }
            catch (Exception ex)
            {
                ContentDialog errorDialog = new ContentDialog
                {
                    Title = "Erro ao inicializar o banco de dados",
                    Content = $"Ocorreu um erro ao inicializar o banco de dados: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = _window.Content.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }
    }
}
