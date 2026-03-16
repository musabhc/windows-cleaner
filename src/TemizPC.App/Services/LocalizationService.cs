using System.Globalization;
using System.Reflection;
using System.Resources;

namespace TemizPC.App.Services;

public sealed class LocalizationService
{
    private readonly ResourceManager _resourceManager =
        new("TemizPC.App.Resources.Strings", Assembly.GetExecutingAssembly());

    public LocalizationService()
    {
        CurrentCulture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("tr", StringComparison.OrdinalIgnoreCase)
            ? CultureInfo.GetCultureInfo("tr-TR")
            : CultureInfo.GetCultureInfo("en-US");
    }

    public event EventHandler? LanguageChanged;

    public CultureInfo CurrentCulture { get; private set; }

    public bool IsTurkish => CurrentCulture.TwoLetterISOLanguageName.Equals("tr", StringComparison.OrdinalIgnoreCase);

    public string Get(string key)
    {
        var localizedValue = _resourceManager.GetString(key, CurrentCulture);
        if (!string.IsNullOrWhiteSpace(localizedValue))
        {
            return localizedValue;
        }

        var fallbackValue = _resourceManager.GetString(key, CultureInfo.InvariantCulture);
        return string.IsNullOrWhiteSpace(fallbackValue) ? key : fallbackValue;
    }

    public string Format(string key, params object[] args)
    {
        return string.Format(CurrentCulture, Get(key), args);
    }

    public void SetCulture(string cultureName)
    {
        var newCulture = CultureInfo.GetCultureInfo(cultureName);
        if (Equals(newCulture, CurrentCulture))
        {
            return;
        }

        CurrentCulture = newCulture;
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }
}
