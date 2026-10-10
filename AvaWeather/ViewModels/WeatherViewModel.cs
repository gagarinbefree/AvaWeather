using Application.Localization;
using Avalonia.Media;
using AvaWeather.Localization;
using AvaWeather.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;

namespace AvaWeather.ViewModels;

public partial class WeatherViewModel(IWeatherRepository repository, WeatherLocationMonitor? locationMonitor = null) : ObservableObject
{
    private bool checkingLocation;
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
            ApplyWeather(locationMonitor is null
                ? await repository.GetWeatherAsync()
                : await locationMonitor.LoadAsync());
        }
        catch (HttpRequestException error)
        {
            ErrorMessage = error.StatusCode is null ? Strings.ConnectionError : error.Message;
        }
        catch (Exception)
        {
            ErrorMessage = Strings.UnexpectedError;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task CheckLocationAsync()
    {
        if (locationMonitor is null || IsLoading || checkingLocation) return;
        checkingLocation = true;
        try
        {
            var updated = await locationMonitor.CheckAsync();
            if (updated is not null) ApplyWeather(updated);
        }
        catch (Exception)
        {
            // Keep the last valid forecast; the next tick retries location detection.
        }
        finally { checkingLocation = false; }
    }

    private void ApplyWeather(WeatherData result)
    {
        Strings = UiStrings.For(WeatherLanguage.ForCountry(result.Country));
        var display = WeatherDisplay.From(result, Strings);
        Weather = result;
        Display = display;
        ErrorMessage = null;
    }
}
