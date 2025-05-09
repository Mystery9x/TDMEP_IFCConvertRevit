using System;
using System.Globalization;
using System.Windows.Data;

namespace TepscoIFCToRevit.UI.ViewModels.Converter
{
    public class RadioBoolToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((string)parameter == value.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
                return parameter;

            return Binding.DoNothing;
        }
    }
}