using Microsoft.UI.Xaml.Controls;
using MinecraftTracker.ViewModels;

namespace MinecraftTracker.Pages
{
    public sealed partial class StatsPage : Page
    {
        public StatsPage()
        {
            this.InitializeComponent();

            // Uma nova instância a cada navegação recalcula tudo do zero a partir
            // do histórico atual — leve o suficiente para não precisar cachear.
            this.DataContext = new StatsViewModel();
        }
    }
}
