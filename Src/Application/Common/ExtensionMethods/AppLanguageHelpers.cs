using System.Globalization;

namespace Application.Common.ExtensionMethods;

public static class AppLanguageHelpers
{
    public static string ToLanguageCode(this ValueObjects.AppLanguage language)
    {
        return language switch
        {
            ValueObjects.AppLanguage.Pl => "PL",
            ValueObjects.AppLanguage.En => "EN",
            _ => "EN"
        };
    }

    public static ValueObjects.AppLanguage FromString(string languageCode)
    {
        return languageCode?.ToUpperInvariant() switch
        {
            "PL" => ValueObjects.AppLanguage.Pl,
            "EN" => ValueObjects.AppLanguage.En,
            _ => ValueObjects.AppLanguage.En
        };
    }
}
