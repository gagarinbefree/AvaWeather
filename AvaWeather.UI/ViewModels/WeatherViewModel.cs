using Application.Localization;
using Application.Diagnostics;
using Avalonia.Media;
using AvaWeather.Localization;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;

namespace AvaWeather.ViewModels;

public partial class WeatherViewModel(IWeatherRepository repository,
    IWeatherFailureReporter? failureReporter = null) : ObservableObject
{
    private readonly IWeatherFailureReporter _failureReporter = failureReporter ?? DisabledWeatherFailureReporter.Instance;
    private WeatherFailureKind _failureKind = WeatherFailureKind.Unexpected;
    [ObservableProperty] private WeatherData? weather;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private WeatherDisplay? display;
    [ObservableProperty] private UiStrings strings = UiStrings.For(WeatherLanguage.ForCountry(null));

    public bool HasWeather => Display is not null && !IsLoading;
    public bool HasError => ErrorMessage is not null && !IsLoading;
    public IBrush HeaderBackground => Display?.HeaderBackground ?? WeatherCardPalette.NeutralHeader;
    public string HeaderLocation => !string.IsNullOrWhiteSpace(Weather?.Name)
        ? Weather.Name : HasError ? Strings.LocationUnavailable : Strings.DetectingLocation;

    partial void OnStringsChanged(UiStrings value) => OnPropertyChanged(nameof(HeaderLocation));

    partial void OnWeatherChanged(WeatherData? value) => OnPropertyChanged(nameof(HeaderLocation));
    partial void OnDisplayChanged(WeatherDisplay? value)
    {
        OnPropertyChanged(nameof(HasWeather));
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
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(HeaderLocation));
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        ErrorMessage = null;
        Weather = null;
        Display = null;
        try
        {
            var result = await repository.GetWeatherAsync();
            Strings = UiStrings.For(WeatherLanguage.ForCountry(result.Country));
            var display = WeatherDisplay.From(result, Strings);
            Weather = result;
            Display = display;
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

    private void ReportFailure()
    {
        try { _failureReporter.Report(_failureKind); }
        catch { /* Diagnostics must never prevent the error state or Retry button. */ }
    }
}
