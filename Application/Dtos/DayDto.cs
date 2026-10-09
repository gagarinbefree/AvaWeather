using System.Text.Json.Serialization;

namespace Application.Dtos;

public class DayDto
{
    [JsonPropertyName("maxtemp_c")]
    public double MaxtempC { get; set; }

    [JsonPropertyName("mintemp_c")]
    public double MintempC { get; set; }

    [JsonPropertyName("avgtemp_c")]
    public double AvgtempC { get; set; }

    [JsonPropertyName("condition")]
    public ConditionDto Condition { get; set; } = new();

    [JsonPropertyName("maxwind_kph")]
    public double MaxwindKph { get; set; }

    [JsonPropertyName("totalprecip_mm")]
    public double TotalprecipMm { get; set; }

    [JsonPropertyName("avghumidity")]
    public int Avghumidity { get; set; }

    [JsonPropertyName("uv")]
    public double Uv { get; set; }

    [JsonPropertyName("daily_chance_of_rain")]
    public int DailyChanceOfRain { get; set; }
}