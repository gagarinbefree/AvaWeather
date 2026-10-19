using Application.Localization;
using Application.Diagnostics;
using Avalonia.Media;
using AvaWeather.Localization;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;
using Application.Caching;

namespace AvaWeather.ViewModels;

public partial class WeatherViewModel(IWeatherRepository repository,
    IWeatherFailureReporter? failureReporter = null, IWeatherCache? cache = null) : ObservableObject
{
    private readonly IWeatherFailureReporter _failureReporter = failureReporter ?? DisabledWeatherFailureReporter.Instance;
    private WeatherFailureKind _failureKind = WeatherFailureKind.Unexpected;
    private DateTimeOffset? _weatherReceivedAt;
    [ObservableProperty] private WeatherData? weather;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private WeatherDisplay? display;
    [ObservableProperty] private UiStrings strings = UiStrings.For(WeatherLanguage.ForCountry(null));
    [ObservableProperty] private bool isCachedWeather;
    [ObservableProperty] private string cacheStatus = string.Empty;

    public bool HasWeather => Display is not null;
    public bool HasInitialLoading => IsLoading && !HasWeather;
    public bool IsRefreshingCached => IsLoading && IsCachedWeather;
    public string HourlySubtitle => IsCachedWeather ? Strings.Get("SavedForecastDates") : Strings.TodayTomorrow;
    public bool HasError => ErrorMessage is not null && !IsLoading;
    public IBrush HeaderBackground => Display?.HeaderBackground ?? WeatherCardPalette.NeutralHeader;
    public string HeaderLocation => !string.IsNullOrWhiteSpace(Weather?.Name)
        ? Weather.Name : HasError ? Strings.LocationUnavailable : Strings.DetectingLocation;

    partial void OnStringsChanged(UiStrings value)
    {
        OnPropertyChanged(nameof(HeaderLocation));
        OnPropertyChanged(nameof(HourlySubtitle));
    }

    partial void OnWeatherChanged(WeatherData? value) => OnPropertyChanged(nameof(HeaderLocation));
    partial void OnDisplayChanged(WeatherDisplay? value)
    {
        OnPropertyChanged(nameof(HasWeather));
        OnPropertyChanged(nameof(HasInitialLoading));
        OnPropertyChanged(nameof(HeaderBackground));
    }
    partial void OnErrorMessageChanged(string? value)
    {
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(HeaderLocation));
    }
    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(HasWeather));
        OnPropertyChanged(nameof(HasInitialLoading));
        OnPropertyChanged(nameof(IsRefreshingCached));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(HeaderLocation));
    }
    partial void OnIsCachedWeatherChanged(bool value)
    {
        OnPropertyChanged(nameof(IsRefreshingCached));
        OnPropertyChanged(nameof(HourlySubtitle));
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            if (Weather is not null && _weatherReceivedAt is { } receivedAt)
                ShowSavedWeather(Weather, receivedAt);
            if (Display is null && cache is not null)
            {
                try
                {
                    var saved = await cache.LoadAsync();
                    if (saved is not null)
                    {
                        _weatherReceivedAt = saved.FetchedAtUtc;
                        ShowSavedWeather(saved.Weather, saved.FetchedAtUtc);
                    }
                }
                catch (Exception) { /* A cache failure must not block fresh weather. */ }
            }
            var result = await repository.GetWeatherAsync();
            var freshStrings = UiStrings.For(WeatherLanguage.ForCountry(result.Country));
            var display = WeatherDisplay.From(result, freshStrings);
            var fetchedAt = DateTimeOffset.UtcNow;
            Strings = freshStrings;
            Weather = result;
            Display = display;
            _weatherReceivedAt = fetchedAt;
            IsCachedWeather = false;
            CacheStatus = string.Empty;
            if (cache is not null)
            {
                try { await cache.SaveAsync(result, fetchedAt, WeatherLanguage.ApiLanguage(Strings.Culture)); }
                catch (Exception) { /* A cache failure must not hide fresh weather. */ }
            }
        }
        catch (HttpRequestException error)
        {
            _failureKind = error.StatusCode is null ? WeatherFailureKind.Connection : WeatherFailureKind.Http;
            ErrorMessage = error.StatusCode is null ? Strings.ConnectionError : error.Message;
            ReportFailure();
        }
        catch (OperationCanceledException)
        {
            _failureKind = WeatherFailureKind.Timeout;
            ErrorMessage = Strings.ConnectionError;
            ReportFailure();
        }
        catch (Exception)
        {
            _failureKind = WeatherFailureKind.Unexpected;
            ErrorMessage = Strings.UnexpectedError;
            ReportFailure();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ShowSavedWeather(WeatherData saved, DateTimeOffset receivedAt)
    {
        var savedStrings = UiStrings.For(WeatherLanguage.ForCountry(saved.Country));
        var savedDisplay = WeatherDisplay.From(saved, savedStrings, historical: true);
        Strings = savedStrings;
        CacheStatus = string.Format(savedStrings.Culture, savedStrings.Get("CachedForecastReceived"),
            receivedAt.ToLocalTime().ToString("g", savedStrings.Culture));
        IsCachedWeather = true;
        Weather = saved;
        Display = savedDisplay;
    }

    private void ReportFailure()
    {
        try { _failureReporter.Report(_failureKind); }
        catch { /* Diagnostics must never prevent the error state or Retry button. */ }
    }
}
