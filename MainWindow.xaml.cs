using System;
using System.Diagnostics;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MinecraftTracker.Pages;
using MinecraftTracker.Services;
using MinecraftTracker.ViewModels;
using MinecraftTracker.Pages;
using MinecraftTracker.Services;
using MinecraftTracker.ViewModels;
using Windows.Media.Core;
using Windows.Media.Playback;
using WinRT.Interop;

namespace MinecraftTracker
{
    public sealed partial class MainWindow : Window
    {
        private AppWindow? _appWindow;
        private MediaPlayer? _mediaPlayer;

        // Permite que outras páginas (ex.: Configurações) encontrem a janela atual
        // para aplicar um novo tema sem precisar de um serviço de navegação completo.
        public static MainWindow? Instance { get; private set; }

        public MainWindow()
        {
            Instance = this;

            this.InitializeComponent();
            SetupCustomTitleBar();
            SetupThemeHandling();

            try { TrySetCustomIcon(); } catch { /* Apenas para evitar de fechar em caso de erro */ }
            try
            {
                if (SettingsService.Current.PlayStartupSound)
                {
                    PlayStartUpAudio();
                }
            }
            catch { /* Apenas para evitar de fechar em caso de erro */ }

            AppWindow.Resize(new Windows.Graphics.SizeInt32(810, 750));
            OverlappedPresenter presenter = OverlappedPresenter.Create();
            presenter.IsResizable = false;

            try
            {
                // O construtor do MainViewModel se auto-registra em MainViewModel.Current
                // e já inicia o monitoramento do processo do jogo em segundo plano.
                _ = new MainViewModel();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao carregar ViewModel: {ex.Message}");
            }

            AppWindow.SetPresenter(presenter);

            // Página inicial ao abrir o app
            ContentFrame.Navigate(typeof(HomePage));
        }

        // Configura a barra de título customizada (AppTitleBar definida no XAML)
        private void SetupCustomTitleBar()
        {
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            var hWnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);
        }

        // Aplica o tema salvo (claro/escuro/sistema) e mantém os botões da barra de
        // título (minimizar/maximizar/fechar) sincronizados sempre que o tema resolvido mudar.
        private void SetupThemeHandling()
        {
            if (this.Content is FrameworkElement root)
            {
                root.RequestedTheme = ThemeFromSettingTag(SettingsService.Current.AppTheme);
                root.ActualThemeChanged += (s, e) => ApplyCaptionButtonColors(root.ActualTheme);
                ApplyCaptionButtonColors(root.ActualTheme);
            }
        }

        // Chamado pela página de Configurações quando o usuário troca o tema.
        public void ApplyTheme(string themeTag)
        {
            if (this.Content is FrameworkElement root)
            {
                root.RequestedTheme = ThemeFromSettingTag(themeTag);
                // root.ActualThemeChanged (configurado acima) cuida de atualizar
                // automaticamente as cores dos botões da barra de título.
            }
        }

        private static ElementTheme ThemeFromSettingTag(string tag) => tag switch
        {
            "Light" => ElementTheme.Light,
            "Dark" => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        private void ApplyCaptionButtonColors(ElementTheme actualTheme)
        {
            if (_appWindow?.TitleBar is null) return;

            bool isDark = actualTheme == ElementTheme.Dark;
            var foreground = isDark
                ? Windows.UI.Color.FromArgb(255, 242, 245, 250)
                : Windows.UI.Color.FromArgb(255, 23, 26, 33);

            _appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            _appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            _appWindow.TitleBar.ButtonForegroundColor = foreground;
        }

        private void TrySetCustomIcon()
        {
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                IntPtr hWnd = WindowNative.GetWindowHandle(this);
                WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
                AppWindow appWindow = AppWindow.GetFromWindowId(wndId);

                // Caminho para o arquivo .ico na pasta do aplicativo
                string iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "MinecraftTracker_icone.ico");
                appWindow.SetIcon(iconPath);
            }
        }

        private void PlayStartUpAudio()
        {
            _mediaPlayer = new MediaPlayer();
            var uri = new Uri("ms-appx:///Assets/Sounds/startup.wav");
            _mediaPlayer.Source = MediaSource.CreateFromUri(uri);
            _mediaPlayer.Play();
        }

        // Troca o conteúdo do Frame de acordo com o item selecionado no menu lateral.
        private void MainNav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItemContainer is NavigationViewItem item && item.Tag is string tag)
            {
                Type? pageType = tag switch
                {
                    "home" => typeof(HomePage),
                    "games" => typeof(GamesPage),
                    "stats" => typeof(StatsPage),
                    "settings" => typeof(SettingsPage),
                    _ => null
                };

                if (pageType != null && ContentFrame.CurrentSourcePageType != pageType)
                {
                    ContentFrame.Navigate(pageType);
                }
            }
        }
    }
}
