using System.Globalization;
using Weather.Localization;

namespace AvaWeather.Tests;

public class StringLocalizerTests
{
    [Theory]
    [InlineData("ru-RU", "Ощущается как", "Влажность", "Установка виджета AvaWeather")]
    [InlineData("en-US", "Feels like", "Humidity", "Install AvaWeather widget")]
    public void Strings_are_resolved_by_key_for_each_supported_language(
        string culture, string feelsLike, string humidity, string installerTitle)
    {
        var localizer = StringLocalizer.For(CultureInfo.GetCultureInfo(culture));

        Assert.Equal(feelsLike, localizer.Get("WidgetFeelsLike"));
        Assert.Equal(humidity, localizer.Get("Humidity"));
        Assert.Equal(installerTitle, localizer.Get("InstallerTitle"));
    }

    [Fact]
    public void Missing_key_fails_instead_of_showing_a_key_to_users()
    {
        Assert.Throws<System.Resources.MissingManifestResourceException>(() =>
            StringLocalizer.For(CultureInfo.GetCultureInfo("en-US")).Get("DoesNotExist"));
    }

    [Theory]
    [InlineData("ru-RU", "Не удалось получить текущую погоду: BadRequest", "Проверка целостности установочного пакета не пройдена.")]
    [InlineData("en-US", "Failed to get current weather: BadRequest", "Installer payload integrity check failed.")]
    public void Non_ui_messages_are_localized(string culture, string weatherError, string installerError)
    {
        var localizer = StringLocalizer.For(CultureInfo.GetCultureInfo(culture));
        Assert.Equal(weatherError, localizer.Format("WeatherApiCurrentFailed", System.Net.HttpStatusCode.BadRequest));
        Assert.Equal(installerError, localizer.Get("InstallerPayloadIntegrityFailed"));
    }
}
