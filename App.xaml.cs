using System;
using System.Diagnostics;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MinecraftTracker.Database;

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
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                _window = new MainWindow();
                _window.Activate();
            }
            catch (Exception ex)
            {
                LogStartupFailure(ex);
                return;
            }

            // Instância o banco de dados
            try
            {
                await DatabaseService.InitializeDatabaseAsync();
            }
            catch (Exception ex)
            {
                LogStartupFailure(ex);

                if (_window.Content is FrameworkElement root && root.XamlRoot != null)
                {
                    ContentDialog errorDialog = new ContentDialog
                    {
                        Title = "Erro ao inicializar o banco de dados",
                        Content = $"Ocorreu um erro ao inicializar o banco de dados: {ex.Message}",
                        CloseButtonText = "OK",
                        XamlRoot = root.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                }
            }
        }

        private static void OnUnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogStartupFailure(ex);
            }
        }

        private static void LogStartupFailure(Exception exception)
        {
            try
            {
                string path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MinecraftTracker",
                    "startup-error.log");
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.AppendAllText(path,
                    $"[{DateTime.Now:O}] {exception}\r\n\r\n");
                Debug.WriteLine(exception);
            }
            catch
            {
                // O log não pode provocar uma segunda falha durante a inicialização.
            }
        }
    }
}
