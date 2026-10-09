namespace Domain.Entities;

public class WeatherData
{
    public string Name { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lon { get; set; }
    public string TzId { get; set; } = string.Empty;
    public DateTime LocalTime { get; set; }

    public CurrentWeather? Current { get; set; }
    public List<HourlyForecast> HourlyForecast { get; set; } = new();
    public List<DailyForecast> DailyForecast { get; set; } = new();
}