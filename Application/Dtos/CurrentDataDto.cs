using System.Text.Json.Serialization;

namespace Application.Dtos;

public class CurrentDataDto
{
    [JsonPropertyName("last_updated")]
    public string LastUpdated { get; set; } = string.Empty;

    [JsonPropertyName("temp_c")]
    public double TempC { get; set; }

    [JsonPropertyName("feelslike_c")]
    public double FeelslikeC { get; set; }

    [JsonPropertyName("condition")]
    public ConditionDto Condition { get; set; } = new();

    [JsonPropertyName("wind_kph")]
    public double WindKph { get; set; }

    [JsonPropertyName("wind_dir")]
    public string WindDir { get; set; } = string.Empty;

    [JsonPropertyName("humidity")]
    public int Humidity { get; set; }

    [JsonPropertyName("pressure_mb")]
    public double PressureMb { get; set; }

    [JsonPropertyName("precip_mm")]
    public double PrecipMm { get; set; }

    [JsonPropertyName("cloud")]
    public int Cloud { get; set; }

    [JsonPropertyName("uv")]
    public double Uv { get; set; }

    [JsonPropertyName("is_day")]
    public int IsDay { get; set; }

    [JsonPropertyName("vis_km")]
    public double VisKm { get; set; }

    [JsonPropertyName("chance_of_rain")]
    public int ChanceOfRain { get; set; }
}