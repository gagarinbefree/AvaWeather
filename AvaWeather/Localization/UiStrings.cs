using System.Globalization;
using Weather.Localization;

namespace AvaWeather.Localization;

public sealed class UiStrings
{
    private UiStrings(CultureInfo culture)
    {
        Culture = culture;
        var localizer = StringLocalizer.For(culture);
        string Get(string key) => localizer.Get(key);

        LoadingWeather = Get(nameof(LoadingWeather));
        Retry = Get(nameof(Retry));
        FeelsLike = Get(nameof(FeelsLike));
        Humidity = Get(nameof(Humidity));
        Wind = Get(nameof(Wind));
        UvIndex = Get(nameof(UvIndex));
        Pressure = Get(nameof(Pressure));
        Visibility = Get(nameof(Visibility));
        HourlyForecast = Get(nameof(HourlyForecast));
        TodayTomorrow = Get(nameof(TodayTomorrow));
        ThreeDayForecast = Get(nameof(ThreeDayForecast));
        Now = Get(nameof(Now));
        Rain = Get(nameof(Rain));
        Uv = Get(nameof(Uv));
        SunriseSunset = Get(nameof(SunriseSunset));
        Today = Get(nameof(Today));
        Tomorrow = Get(nameof(Tomorrow));
        LocationUnavailable = Get(nameof(LocationUnavailable));
        DetectingLocation = Get(nameof(DetectingLocation));
        ViewUnavailable = Get(nameof(ViewUnavailable));
        PlaceNameCredit = Get(nameof(PlaceNameCredit));
        ConnectionError = Get(nameof(ConnectionError));
        UnexpectedError = Get(nameof(UnexpectedError));
        KilometersPerHour = Get(nameof(KilometersPerHour));
        Millibars = Get(nameof(Millibars));
        Kilometers = Get(nameof(Kilometers));
    }

    public CultureInfo Culture { get; }
    public string LoadingWeather { get; }
    public string Retry { get; }
    public string FeelsLike { get; }
    public string Humidity { get; }
    public string Wind { get; }
    public string UvIndex { get; }
    public string Pressure { get; }
    public string Visibility { get; }
    public string HourlyForecast { get; }
    public string TodayTomorrow { get; }
    public string ThreeDayForecast { get; }
    public string Now { get; }
    public string Rain { get; }
    public string Uv { get; }
    public string SunriseSunset { get; }
    public string Today { get; }
    public string Tomorrow { get; }
    public string LocationUnavailable { get; }
    public string DetectingLocation { get; }
    public string ViewUnavailable { get; }
    public string PlaceNameCredit { get; }
    public string ConnectionError { get; }
    public string UnexpectedError { get; }
    public string KilometersPerHour { get; }
    public string Millibars { get; }
    public string Kilometers { get; }

    public static UiStrings For(CultureInfo culture) => new(culture);
    public string Get(string key) => StringLocalizer.For(Culture).Get(key);
}
