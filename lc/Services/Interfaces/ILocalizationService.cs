using System;
using System.Linq;
using System.Windows;

namespace lc.Services.Interfaces
{
    public interface ILocalizationService
    {
        void SetLanguage(string langCode);
    }

    public class LocalizationService : ILocalizationService
    {
        public void SetLanguage(string langCode)
        {
            var dictionaries = Application.Current.Resources.MergedDictionaries;

            var langDictionary = dictionaries.FirstOrDefault(d =>
                d.Source != null &&
                d.Source.OriginalString.Contains("/Resources/Localisation/Language."));

            if (langDictionary != null)
            {
                int index = dictionaries.IndexOf(langDictionary);
                dictionaries.Remove(langDictionary);

                var newLang = new ResourceDictionary
                {
                    Source = new Uri($"/Resources/Localisation/Language.{langCode}.xaml", UriKind.Relative)
                };

                dictionaries.Insert(index, newLang);
            }
        }
    }
}