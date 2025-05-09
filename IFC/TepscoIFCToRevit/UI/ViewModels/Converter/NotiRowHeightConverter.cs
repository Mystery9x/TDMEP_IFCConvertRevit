using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TepscoIFCToRevit.UI.ViewModels.Converter
{
    public class NotiRowHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double height = 0;
            if (value is VMSettingCategory category)
            {
                if (!string.IsNullOrEmpty(category.Notification))
                    height = 45;
            }
            GridLength controlHeight = new GridLength(height, GridUnitType.Pixel);
            return controlHeight;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}