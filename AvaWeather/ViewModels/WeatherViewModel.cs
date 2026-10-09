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

    public bool HasWeather => Display is not null && !IsLoading;
    public bool HasError => ErrorMessage is not null && !IsLoading;

    partial void OnDisplayChanged(WeatherDisplay? value) => OnPropertyChanged(nameof(HasWeather));
    partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasError));
    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(HasWeather));
        OnPropertyChanged(nameof(HasError));
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
            var display = WeatherDisplay.From(result);
            Weather = result;
            Display = display;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Failed to connect to weather service. Please check your internet connection.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"An error occurred: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
