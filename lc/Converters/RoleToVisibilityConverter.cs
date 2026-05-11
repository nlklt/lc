using System.Globalization;
using System.Windows;
using System.Windows.Data;
using lc.Models.Enums;

namespace lc.Converters
{
    public class RoleToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            string? currentUserRole = value.ToString();
            string? requiredRole = parameter.ToString();

            if (currentUserRole != null && currentUserRole.Equals(requiredRole, StringComparison.OrdinalIgnoreCase))
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}