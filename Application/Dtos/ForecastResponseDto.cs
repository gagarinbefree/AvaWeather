using System.Text.Json.Serialization;

namespace Application.Dtos;

public class ForecastResponseDto
{
    [JsonPropertyName("location")]
    public LocationDto Location { get; set; } = new();

    [JsonPropertyName("forecast")]
    public ForecastDto Forecast { get; set; } = new();
}