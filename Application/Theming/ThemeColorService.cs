using System.Text.Json;
using System.Text.Json.Serialization;
using Weather.Localization;

namespace Application.Theming;

public sealed class ThemeColorService
{
    private readonly IReadOnlyDictionary<string, string> _colors;

    public static ThemeColorService Default { get; } = new("default");
    public string Name { get; }

    public ThemeColorService(string name)
    {
        Name = name;
        var resourceName = $"Application.Theming.Themes.{name}.json";
        using var stream = typeof(ThemeColorService).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidDataException(StringLocalizer.Current.Format("ThemeResourceMissing", name));
        ThemeDocument? document;
        try
        {
            document = JsonSerializer.Deserialize(stream, ThemeJsonContext.Default.ThemeDocument);
        }
        catch (JsonException error)
        {
            throw new InvalidDataException(StringLocalizer.Current.Format("ThemeResourceInvalid", name), error);
        }
        if (document is null || document.Name != name || document.Colors is null || document.Colors.Count == 0 ||
            document.Colors.Any(color => string.IsNullOrWhiteSpace(color.Key) || !IsHexColor(color.Value)))
            throw new InvalidDataException(StringLocalizer.Current.Format("ThemeResourceInvalid", name));

        _colors = new Dictionary<string, string>(document.Colors, StringComparer.Ordinal);
    }

    public string GetHex(string name) => _colors.TryGetValue(name, out var value)
        ? value : throw new KeyNotFoundException(StringLocalizer.Current.Format("ThemeColorMissing", name));

    private static bool IsHexColor(string? value) =>
        value is { Length: 7 or 9 } && value[0] == '#' &&
        value.AsSpan(1).IndexOfAnyExcept("0123456789ABCDEFabcdef") < 0;
}

internal sealed class ThemeDocument
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, string> Colors { get; set; } = [];
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(ThemeDocument))]
internal partial class ThemeJsonContext : JsonSerializerContext;
