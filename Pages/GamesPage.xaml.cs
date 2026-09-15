using System;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;
using MinecraftTracker.Models;
using MinecraftTracker.ViewModels;

namespace MinecraftTracker.Pages
{
    public sealed partial class GamesPage : Page
    {
        private readonly GamesViewModel _viewModel = new();

        public GamesPage()
        {
            this.InitializeComponent();

            GamesListView.ItemsSource = _viewModel.Games;
            _viewModel.Games.CollectionChanged += (s, e) => UpdateEmptyState();
            UpdateEmptyState();
        }

        private void UpdateEmptyState()
        {
            EmptyStateBorder.Visibility = _viewModel.Games.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private async void AddGameButton_Click(object sender, RoutedEventArgs e)
        {
            var nameBox = new TextBox { Header = "Nome do jogo", PlaceholderText = "Ex.: Minecraft Java Edition" };

            var pathBox = new TextBox { Header = "Executável", PlaceholderText = "Nenhum executável selecionado", IsReadOnly = true, Width = 250 };
            var browseButton = new Button { Content = "Procurar...", VerticalAlignment = VerticalAlignment.Bottom };

            var processBox = new TextBox
            {
                Header = "Nome do processo a monitorar",
                PlaceholderText = "ex.: javaw, Minecraft.Windows"
            };

            browseButton.Click += async (s, args) =>
            {
                var picker = new FileOpenPicker();
                var hwnd = WindowNative.GetWindowHandle(MainWindow.Instance);
                InitializeWithWindow.Initialize(picker, hwnd);
                picker.FileTypeFilter.Add(".exe");

                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    pathBox.Text = file.Path;

                    if (string.IsNullOrWhiteSpace(nameBox.Text))
                    {
                        nameBox.Text = Path.GetFileNameWithoutExtension(file.Path);
                    }

                    if (string.IsNullOrWhiteSpace(processBox.Text))
                    {
                        // Sugestão inicial — o usuário pode ajustar, já que o processo real
                        // às vezes tem outro nome (ex.: um launcher que abre outro processo).
                        processBox.Text = Path.GetFileNameWithoutExtension(file.Path);
                    }
                }
            };

            var pathRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            pathRow.Children.Add(pathBox);
            pathRow.Children.Add(browseButton);

            var hint = new TextBlock
            {
                Text = "Dica: se o jogo abre por um launcher, o processo monitorado pode ter um nome diferente do executável escolhido acima.",
                FontSize = 11,
                Opacity = 0.7,
                TextWrapping = TextWrapping.Wrap
            };

            var content = new StackPanel { Spacing = 14, Width = 360 };
            content.Children.Add(nameBox);
            content.Children.Add(pathRow);
            content.Children.Add(processBox);
            content.Children.Add(hint);

            var dialog = new ContentDialog
            {
                Title = "Adicionar jogo",
                PrimaryButtonText = "Salvar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                Content = content,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(nameBox.Text) || string.IsNullOrWhiteSpace(pathBox.Text))
            {
                // TODO: mostrar validação visual (ex.: InfoBar) em vez de apenas ignorar o salvamento
                return;
            }

            await _viewModel.AddGameAsync(
                nameBox.Text.Trim(),
                pathBox.Text.Trim(),
                string.IsNullOrWhiteSpace(processBox.Text)
                    ? Path.GetFileNameWithoutExtension(pathBox.Text)
                    : processBox.Text.Trim());
        }

        private void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is GameShortcutItem item)
            {
                _viewModel.LaunchGame(item);
            }
        }

        private async void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not GameShortcutItem item)
            {
                return;
            }

            var dialog = new ContentDialog
            {
                Title = "Remover jogo",
                Content = $"Remover o atalho de \"{item.Name}\"? Isso não apaga o histórico de sessões já registrado.",
                PrimaryButtonText = "Remover",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await _viewModel.RemoveGameAsync(item);
            }
        }
    }
}
