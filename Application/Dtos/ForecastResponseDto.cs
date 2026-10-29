using System.Text.Json.Serialization;

namespace Application.Dtos;

public class ForecastResponseDto : IWeatherResponseMetadata
{
    [JsonIgnore]
    public DateTimeOffset? ServerCachedAtUtc { get; set; }

    [JsonPropertyName("location")]
    public LocationDto Location { get; set; } = new();

    [JsonPropertyName("current")]
    public CurrentDataDto Current { get; set; } = new();

    [JsonPropertyName("forecast")]
    public ForecastDto Forecast { get; set; } = new();
}
