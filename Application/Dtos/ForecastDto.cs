using System.Text.Json.Serialization;

namespace Application.Dtos;

public class ForecastDto
{
    [JsonPropertyName("forecastday")]
    public List<ForecastDayDto> ForecastDay { get; set; } = new();
}