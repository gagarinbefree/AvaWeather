using System.Text.Json;
using Weather.Localization;

namespace Application.Weather;

public sealed record WeatherConditionDescriptor(string Kind, string Icon, string WidgetSymbol);

public static class WeatherConditionCatalog
{
    private static readonly (Dictionary<int, (string Day, string Night)> Codes,
        Dictionary<string, WeatherConditionDescriptor> Kinds) Catalog = Load();

    public static WeatherConditionDescriptor For(int code, bool isDay)
    {
        var kind = Catalog.Codes.TryGetValue(code, out var match)
            ? isDay ? match.Day : match.Night
            : "PartlyCloudy";
        return Catalog.Kinds[kind];
    }

    private static (Dictionary<int, (string Day, string Night)>,
        Dictionary<string, WeatherConditionDescriptor>) Load()
    {
        using var stream = typeof(WeatherConditionCatalog).Assembly
            .GetManifestResourceStream("Application.weather-conditions.json")
            ?? throw new InvalidDataException(StringLocalizer.Current.Get("ConditionCatalogMissing"));
        using var document = JsonDocument.Parse(stream);
        var codes = new Dictionary<int, (string, string)>();
        var kinds = new Dictionary<string, WeatherConditionDescriptor>(StringComparer.Ordinal);
        foreach (var entry in document.RootElement.EnumerateArray())
        {
            var kind = entry.GetProperty("kind").GetString()!;
            var night = entry.TryGetProperty("nightKind", out var nightKind)
                ? nightKind.GetString()! : kind;
            kinds.Add(kind, new WeatherConditionDescriptor(kind,
                entry.GetProperty("icon").GetString()!,
                entry.GetProperty("widgetSymbol").GetString()!));
            foreach (var code in entry.GetProperty("codes").EnumerateArray())
                codes.Add(code.GetInt32(), (kind, night));
        }
        return (codes, kinds);
    }
}
