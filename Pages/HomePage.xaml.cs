using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MinecraftTracker.ViewModels;

namespace MinecraftTracker.Pages
{
    public sealed partial class HomePage : Page
    {
        public HomePage()
        {
            this.InitializeComponent();

            // Reaproveita a mesma instância do ViewModel usada pelo monitoramento
            // em segundo plano, para não perder o estado ao navegar entre páginas.
            this.DataContext = MainViewModel.Current;
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
                // e então recarregar as estatísticas do dashboard:
                // await DatabaseService.ClearHistoryAsync();
                if (MainViewModel.Current != null)
                {
                    await MainViewModel.Current.ReloadAsync();
                }
            }
        }
    }
}
