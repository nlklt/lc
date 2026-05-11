using System.Globalization;
using System.Windows.Data;

namespace lc.Converters
{
    public class NullableDoubleToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is double d ? d.ToString(culture) : string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value?.ToString()?.Trim();

            if (string.IsNullOrWhiteSpace(text))
                return null!;

            if (double.TryParse(text, NumberStyles.Float, culture, out var result))
                return result;

            return Binding.DoNothing;
        }
    }
}