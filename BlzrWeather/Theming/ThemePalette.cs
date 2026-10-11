using System.Text.Json;
using Weather.Localization;

namespace BlzrWeather.Theming;

public sealed class ThemePalette
{
    private readonly IReadOnlyDictionary<string, string> _colors;

    private ThemePalette(IReadOnlyDictionary<string, string> colors) => _colors = colors;

    public string Get(string name) => _colors.TryGetValue(name, out var color)
        ? color : throw new KeyNotFoundException(StringLocalizer.Current.Format("ThemeColorMissing", name));

    public string Current(string kind) => Get($"Weather.{kind}.Current");
    public string Forecast(string kind) => Get($"Weather.{kind}.Forecast");
    public string Accent(string kind) => Get($"Weather.{kind}.Accent");
    public string Header(string kind) =>
        $"linear-gradient(90deg, {Get($"Weather.{kind}.HeaderStart")}, {Get($"Weather.{kind}.HeaderEnd")})";

    public string BaseVariables =>
        $"--page:{Get("Page")};--ink:{Get("Ink")};--muted:{Get("Muted")};--blue:{Get("Blue")};" +
        $"--white:{Get("White")};--divider:{Get("Divider.Section")};" +
        $"--divider-current:{Get("Divider.Current")};--error-bg:{Get("Error.Background")};" +
        $"--error-icon:{Get("Error.Icon")}";

    public static ThemePalette Load()
    {
        using var stream = typeof(ThemePalette).Assembly.GetManifestResourceStream("BlzrWeather.theme.json")
            ?? throw new InvalidDataException(StringLocalizer.Current.Format("ThemeResourceMissing", "default.json"));
        using var document = JsonDocument.Parse(stream);
        var colors = document.RootElement.GetProperty("colors").EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value.GetString()!, StringComparer.Ordinal);
        return new ThemePalette(colors);
    }
}
