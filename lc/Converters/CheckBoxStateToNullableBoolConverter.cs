using lc.Models.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace lc.Converters
{
    public sealed class CheckBoxStateToNullableBoolConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                CheckBoxState.Include => true,
                CheckBoxState.Exclude => false,
                _ => null
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                true => CheckBoxState.Include,
                false => CheckBoxState.Exclude,
                _ => CheckBoxState.Neutral
            };
        }
    }

}
