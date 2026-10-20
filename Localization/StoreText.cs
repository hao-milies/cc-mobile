using System.Globalization;
using System.Resources;
namespace CCMobile.Localization;
public sealed class StoreText
{
    private static readonly ResourceManager Resources = new("CCMobile.Localization.StoreText", typeof(StoreText).Assembly);
    public static bool IsEnglish => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en";
    public static string T(string text) => Resources.GetString(text, CultureInfo.CurrentUICulture) ?? text;
    public static string F(string text, params object?[] args) => string.Format(CultureInfo.CurrentUICulture, T(text), args);
}
