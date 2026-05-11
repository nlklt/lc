using System.Windows;

namespace lc.Services
{
    public interface IThemeService
    {
        void ApplyTheme(string themeName);
    }

    public class ThemeService : IThemeService
    {
        public void ApplyTheme(string themeName)
        {
            var dictionaries = Application.Current.Resources.MergedDictionaries;

            var themeDictionary = dictionaries
                .FirstOrDefault(d => d.Source != null &&
                    (d.Source.OriginalString.Contains("Dark.xaml") || d.Source.OriginalString.Contains("Light.xaml")));

            if (themeDictionary != null)
                dictionaries.Remove(themeDictionary);

            var newTheme = new ResourceDictionary
            {
                Source = new Uri($"Resources/Themes/{themeName}.xaml", UriKind.Relative)
            };

            dictionaries.Insert(0, newTheme);
        }
    }
}