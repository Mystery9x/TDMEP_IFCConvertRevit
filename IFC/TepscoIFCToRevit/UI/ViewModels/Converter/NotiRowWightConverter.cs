using Autodesk.Revit.DB;
using System;
using System.Globalization;
using System.Windows.Data;

namespace TepscoIFCToRevit.UI.ViewModels.Converter
{
    public class NotiRowWightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double width = 0;
            if (value is VMSettingCategory category)
            {
                if (!string.IsNullOrEmpty(category.Notification) && category.ProcessBuiltInCategory == BuiltInCategory.OST_GenericModel)
                    width = 150;
                else
                    width = 250;
            }

            return width;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Windows.Data.Binding.DoNothing;
        }
    }
}