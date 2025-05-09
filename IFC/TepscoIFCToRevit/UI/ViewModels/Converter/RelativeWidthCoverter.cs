using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TepscoIFCToRevit.UI.ViewModels.Converter
{
    internal class RelativeWidthCoverter : IValueConverter
    {
        private Thickness m_Margin = new Thickness(0.0);

        public Thickness Margin
        {
            get { return m_Margin; }
            set { m_Margin = value; }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(double)) { return null; }
            double dParentWidth = Double.Parse(value.ToString());
            double dAdjustedWidth = dParentWidth - m_Margin.Left - 28;
            return (dAdjustedWidth < 0 ? 0 : dAdjustedWidth);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}