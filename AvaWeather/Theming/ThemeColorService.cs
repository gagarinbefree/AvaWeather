using System.Text.Json;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Weather.Localization;

namespace AvaWeather.Theming;

public sealed class ThemeColorService
{
    private sealed class ThemeDocument
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, string> Colors { get; set; } = [];
    }

    private readonly IReadOnlyDictionary<string, string> _colors;
    public static ThemeColorService Default { get; } = new("default");
    public string Name { get; }

    public ThemeColorService(string name)
    {
        Name = name;
        var resourceName = $"AvaWeather.Assets.Themes.{name}.json";
        using var stream = typeof(ThemeColorService).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidDataException(StringLocalizer.Current.Format("ThemeResourceMissing", name));
        var document = JsonSerializer.Deserialize<ThemeDocument>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException(StringLocalizer.Current.Format("ThemeResourceInvalid", name));
        _colors = document.Colors;
    }

    public string GetHex(string name) => _colors.TryGetValue(name, out var value)
        ? value : throw new KeyNotFoundException(StringLocalizer.Current.Format("ThemeColorMissing", name));

    public Color GetColor(string name) => Color.Parse(GetHex(name));
    public IBrush GetBrush(string name) => new ImmutableSolidColorBrush(GetColor(name));
}
