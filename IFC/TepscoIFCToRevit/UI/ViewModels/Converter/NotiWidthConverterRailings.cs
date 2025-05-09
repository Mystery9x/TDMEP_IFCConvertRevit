using Autodesk.Revit.DB;
using System;
using System.Globalization;
using System.Windows.Data;

namespace TepscoIFCToRevit.UI.ViewModels.Converter
{
    public class NotiHeightConverterRailings : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double height = 0;
            if (value is VMSettingCategory category)
            {
                if (category.ProcessBuiltInCategory == BuiltInCategory.OST_Railings)
                    height = 0;
                else
                    height = 40;
            }
            return height;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Windows.Data.Binding.DoNothing;
        }
    }
}