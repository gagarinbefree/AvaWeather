namespace Infrastructure.Configuration;

public class WeatherApiOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DefaultLocation { get; set; } = string.Empty;
    public int ForecastDays { get; set; }
}