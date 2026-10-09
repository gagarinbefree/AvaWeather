using Application.Localization;
using AvaWeather.Localization;
using AvaWeather.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;

namespace AvaWeather.ViewModels;

public partial class WeatherViewModel(IWeatherRepository repository) : ObservableObject
{
    [ObservableProperty] private WeatherData? weather;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private WeatherDisplay? display;
    [ObservableProperty] private UiStrings strings = UiStrings.For(WeatherLanguage.ForCountry(null));

    public bool HasWeather => Display is not null && !IsLoading;
    public bool HasError => ErrorMessage is not null && !IsLoading;
    public string HeaderLocation => !string.IsNullOrWhiteSpace(Weather?.Name)
        ? Weather.Name : HasError ? Strings.LocationUnavailable : Strings.DetectingLocation;

    partial void OnStringsChanged(UiStrings value) => OnPropertyChanged(nameof(HeaderLocation));

    partial void OnWeatherChanged(WeatherData? value) => OnPropertyChanged(nameof(HeaderLocation));
    partial void OnDisplayChanged(WeatherDisplay? value) => OnPropertyChanged(nameof(HasWeather));
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
        catch (HttpRequestException)
        {
            ErrorMessage = Strings.ConnectionError;
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
}
