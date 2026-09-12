using System;
using System.Globalization;
using Microsoft.UI.Xaml.Data; // Ou System.Windows.Data se for WPF padrão

namespace SeuProjeto.Converters
{
    public class BoolToStatusTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isRunning = value is bool b && b;
            return isRunning ? "Executando" : "Inativo";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}