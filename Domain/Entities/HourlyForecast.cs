namespace Domain.Entities;

public class HourlyForecast
{
    public DateTime Time { get; set; }
    public double TempC { get; set; }
    public double FeelslikeC { get; set; }
    public int ConditionCode { get; set; }
    public string ConditionText { get; set; } = string.Empty;
    public string ConditionIcon { get; set; } = string.Empty;
    public double WindKph { get; set; }
    public string WindDir { get; set; } = string.Empty;
    public int Humidity { get; set; }
    public double PrecipMm { get; set; }
    public int ChanceOfRain { get; set; }
    public int IsDay { get; set; }
    public double Uv { get; set; }
}