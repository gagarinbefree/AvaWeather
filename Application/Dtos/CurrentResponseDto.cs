using System.Text.Json.Serialization;

namespace Application.Dtos;

public class CurrentResponseDto : IWeatherResponseMetadata
{
    [JsonIgnore]
    public DateTimeOffset? ServerCachedAtUtc { get; set; }

    [JsonPropertyName("location")]
    public LocationDto Location { get; set; } = new();

    [JsonPropertyName("current")]
    public CurrentDataDto Current { get; set; } = new();
}