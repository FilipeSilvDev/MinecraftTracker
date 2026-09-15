namespace MinecraftTracker.Models
{
    // Representa um dia no gráfico "Últimos 7 dias" da página de Estatísticas.
    public class DayPlaytime
    {
        public string DayLabel { get; set; } = string.Empty;
        public double Minutes { get; set; }

        // Altura já calculada em pixels (proporcional ao dia com mais minutos jogados),
        // pronta para ser usada direto no binding de altura da barra no XAML.
        public double BarHeight { get; set; }

        public string MinutesFormatted => Minutes <= 0
            ? "Sem sessões"
            : $"{(int)(Minutes / 60)}h {(int)(Minutes % 60)}min";
    }
}
