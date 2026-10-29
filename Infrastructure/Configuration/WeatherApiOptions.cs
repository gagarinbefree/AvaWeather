namespace Infrastructure.Configuration;

public class WeatherApiOptions
{
    public string BaseUrl { get; set; } = WeatherBackendConfiguration.BaseUrl;
    public string DefaultLocation { get; set; } = "auto:ip";
    public int ForecastDays { get; set; } = 3;
    public int RequestTimeoutSeconds { get; set; } = 40;
    public bool UseDirectConnectionFallback { get; set; }
}
