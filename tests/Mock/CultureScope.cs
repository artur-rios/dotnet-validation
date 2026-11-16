using System.Globalization;

namespace ArturRios.Validation.Tests.Mock;

/// <summary>
/// Sets the current culture and UI culture for the lifetime of the scope. The suite runs in the invariant culture
/// (xunit.runner.json); a test that needs another one sets it here and gets the invariant one back afterwards.
/// </summary>
public sealed class CultureScope : IDisposable
{
    private readonly CultureInfo _culture = CultureInfo.CurrentCulture;
    private readonly CultureInfo _uiCulture = CultureInfo.CurrentUICulture;

    public CultureScope(CultureInfo culture)
    {
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _culture;
        CultureInfo.CurrentUICulture = _uiCulture;
    }
}
