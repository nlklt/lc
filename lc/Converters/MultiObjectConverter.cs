using System;
using System.Globalization;
using System.Windows.Data;

namespace lc.Converters
{
    public class MultiObjectConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return values.Clone(); // Возвращаем массив переданных объектов
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}