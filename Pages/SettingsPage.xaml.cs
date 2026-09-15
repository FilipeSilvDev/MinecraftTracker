using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MinecraftTracker.Services;
using MinecraftTracker.ViewModels;

namespace MinecraftTracker.Pages
{
    public sealed partial class SettingsPage : Page
    {
        // Evita que o carregamento inicial dos controles (ComboBox/ToggleSwitch)
        // dispare os eventos de mudança e sobrescreva as configurações já salvas.
        private bool _isLoadingSettings = true;

        public SettingsPage()
        {
            this.InitializeComponent();
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            var settings = SettingsService.Current;

            foreach (var obj in ThemeComboBox.Items)
            {
                if (obj is ComboBoxItem item && (string)item.Tag == settings.AppTheme)
                {
                    ThemeComboBox.SelectedItem = item;
                    break;
                }
            }

            StartupSoundToggle.IsOn = settings.PlayStartupSound;
            StartWithWindowsToggle.IsOn = settings.StartWithWindows;

            _isLoadingSettings = false;
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingSettings) return;

            if (ThemeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                SettingsService.Current.AppTheme = tag;
                SettingsService.Save();

                MainWindow.Instance?.ApplyTheme(tag);
            }
        }

        private void StartupSoundToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isLoadingSettings) return;

            SettingsService.Current.PlayStartupSound = StartupSoundToggle.IsOn;
            SettingsService.Save();
        }

        private void StartWithWindowsToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isLoadingSettings) return;

            SettingsService.Current.StartWithWindows = StartWithWindowsToggle.IsOn;
            SettingsService.Save();

            // TODO: como o app é "unpackaged", registrar a inicialização com o Windows
            // exige criar/remover manualmente uma entrada, por exemplo:
            // - um atalho em shell:startup, ou
            // - uma chave em HKCU\Software\Microsoft\Windows\CurrentVersion\Run
        }

        private async void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Limpar histórico",
                Content = "Isso vai apagar todas as sessões registradas. Essa ação não pode ser desfeita.",
                PrimaryButtonText = "Limpar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                // TODO: implementar DatabaseService.ClearHistoryAsync() no banco SQLite
                // await DatabaseService.ClearHistoryAsync();
                if (MainViewModel.Current != null)
                {
                    await MainViewModel.Current.ReloadAsync();
                }
            }
        }
    }
}
