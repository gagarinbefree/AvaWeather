using Avalonia.Media;

namespace AvaWeather.ViewModels;

public sealed record WeatherCardColors(IBrush CurrentBackground, IBrush ForecastBackground, IBrush Accent);

public static class WeatherCardPalette
{
    private static readonly WeatherCardColors Sunny = Colors("#FFF0B8", "#FFF8DF", "#7C5700");
    private static readonly WeatherCardColors PartlyCloudy = Colors("#F4EBD4", "#FBF6E9", "#6D5A2F");
    private static readonly WeatherCardColors Cloudy = Colors("#DFE8EF", "#F1F5F8", "#435B70");
    private static readonly WeatherCardColors Rain = Colors("#D7EAF5", "#EBF5FA", "#255B78");
    private static readonly WeatherCardColors Snow = Colors("#E7EEF8", "#F5F8FC", "#496382");
    private static readonly WeatherCardColors Fog = Colors("#E7EAE5", "#F3F5F1", "#52645B");
    private static readonly WeatherCardColors Thunder = Colors("#E4DFF2", "#F2EFFA", "#5A4F78");
    private static readonly WeatherCardColors Night = Colors("#DCE5F5", "#EEF3FB", "#344E78");

    public static WeatherCardColors For(WeatherConditionKind condition) => condition switch
    {
        WeatherConditionKind.ClearDay => Sunny,
        WeatherConditionKind.PartlyCloudy => PartlyCloudy,
        WeatherConditionKind.Cloudy => Cloudy,
        WeatherConditionKind.Rain => Rain,
        WeatherConditionKind.Snow => Snow,
        WeatherConditionKind.Fog => Fog,
        WeatherConditionKind.Thunder => Thunder,
        WeatherConditionKind.ClearNight or WeatherConditionKind.PartlyCloudyNight => Night,
        _ => PartlyCloudy
    };

    private static WeatherCardColors Colors(string current, string forecast, string accent) =>
        new(Brush.Parse(current), Brush.Parse(forecast), Brush.Parse(accent));
}
