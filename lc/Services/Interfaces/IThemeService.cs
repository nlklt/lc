using System.Windows;

namespace lc.Services.Interfaces
{
    public interface IThemeService
    {
        void SetTheme(string themeName);
    }

    public class ThemeService : IThemeService
    {
        public void SetTheme(string themeName)
        {
            var dictionaries = Application.Current.Resources.MergedDictionaries;

            // Ищем словарь темы более надежно
            var themeDictionary = dictionaries.FirstOrDefault(d =>
                d.Source != null &&
                (d.Source.OriginalString.EndsWith("Dark.xaml", StringComparison.OrdinalIgnoreCase) ||
                 d.Source.OriginalString.EndsWith("Light.xaml", StringComparison.OrdinalIgnoreCase)));

            if (themeDictionary != null)
            {
                int index = dictionaries.IndexOf(themeDictionary);

                // Используем абсолютный Pack URI для надежности
                var uri = new Uri($"pack://application:,,,/Resources/Themes/{themeName}.xaml", UriKind.Absolute);

                // Создаем новый словарь
                var newTheme = new ResourceDictionary { Source = uri };

                // Заменяем
                dictionaries.RemoveAt(index);
                dictionaries.Insert(index, newTheme);
            }
            else
            {
                // Если вдруг словарь не найден (например, при первом запуске), добавляем его в начало
                dictionaries.Insert(0, new ResourceDictionary
                {
                    Source = new Uri($"pack://application:,,,/Resources/Themes/{themeName}.xaml", UriKind.Absolute)
                });
            }
        }
    }
}