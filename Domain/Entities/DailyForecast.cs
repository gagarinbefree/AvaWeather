namespace Domain.Entities;

public class DailyForecast
{
    public DateTime Date { get; set; }
    public double MaxtempC { get; set; }
    public double MintempC { get; set; }
    public double AvgtempC { get; set; }
    public int ConditionCode { get; set; }
    public string ConditionText { get; set; } = string.Empty;
    public string ConditionIcon { get; set; } = string.Empty;
    public double MaxwindKph { get; set; }
    public double TotalprecipMm { get; set; }
    public int Avghumidity { get; set; }
    public double Uv { get; set; }
    public int DailyChanceOfRain { get; set; }
    public string Sunrise { get; set; } = string.Empty;
    public string Sunset { get; set; } = string.Empty;
}