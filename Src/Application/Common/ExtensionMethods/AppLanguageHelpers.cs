using System.Globalization;

namespace Application.Common.ExtensionMethods;

public static class AppLanguageHelpers
{
    public static string ToLanguageCode(this ValueObjects.AppLanguage language)
    {
        return language switch
        {
            ValueObjects.AppLanguage.Pl => "pl",
            ValueObjects.AppLanguage.En => "en",
            _ => "en"
        };
    }

    public static ValueObjects.AppLanguage FromString(string languageCode)
    {
        return languageCode.ToLower(CultureInfo.InvariantCulture) switch
        {
            "pl" => ValueObjects.AppLanguage.Pl,
            "en" => ValueObjects.AppLanguage.En,
            _ => ValueObjects.AppLanguage.En
        };
    }
}
