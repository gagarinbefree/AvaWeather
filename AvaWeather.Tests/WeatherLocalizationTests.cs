using System.Collections;
using System.Globalization;
using System.Resources;
using Application.Localization;
using AvaWeather.Localization;
using Weather.Localization;

namespace AvaWeather.Tests;

public class WeatherLocalizationTests
{
    [Theory]
    [InlineData("Russia")]
    [InlineData("Ukraine")]
    [InlineData("Kazakhstan")]
    [InlineData("Belarus")]
    [InlineData("Kyrgyzstan")]
    [InlineData("Uzbekistan")]
    [InlineData("Moldova")]
    [InlineData("Latvia")]
    public void Russian_speaking_country_selects_russian(string country)
    {
        Assert.Equal("ru-RU", WeatherLanguage.ForCountry(country).Name);
    }

    [Theory]
    [InlineData("United States")]
    [InlineData("Germany")]
    [InlineData("")]
    public void Other_or_unknown_country_selects_english(string country)
    {
        Assert.Equal("en-US", WeatherLanguage.ForCountry(country).Name);
    }

    [Fact]
    public void Resources_contain_translations_for_both_languages()
    {
        var ru = UiStrings.For(WeatherLanguage.ForCountry("Ukraine"));
        var en = UiStrings.For(WeatherLanguage.ForCountry("Germany"));

        Assert.Equal("Почасовой прогноз", ru.HourlyForecast);
        Assert.Equal("Hourly Forecast", en.HourlyForecast);
        Assert.Equal("Сегодня", ru.Today);
        Assert.Equal("Today", en.Today);
        Assert.Equal("Повторить", ru.Retry);
        Assert.Equal("Retry", en.Retry);
        Assert.Equal("Закрыть окно", ru.CloseWindow);
        Assert.Equal("Close window", en.CloseWindow);
    }

    [Fact]
    public void Russian_resource_has_every_english_key()
    {
        var manager = new ResourceManager("Weather.Localization.Strings", typeof(StringLocalizer).Assembly);
        var english = manager.GetResourceSet(CultureInfo.InvariantCulture, true, false)!;
        var russian = manager.GetResourceSet(CultureInfo.GetCultureInfo("ru"), true, false)!;
        var englishKeys = english.Cast<DictionaryEntry>().Select(entry => (string)entry.Key).Order().ToArray();
        var russianKeys = russian.Cast<DictionaryEntry>().Select(entry => (string)entry.Key).Order().ToArray();

        Assert.Equal(englishKeys, russianKeys);
    }

}
