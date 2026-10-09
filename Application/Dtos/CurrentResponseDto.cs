using System.Text.Json.Serialization;

namespace Application.Dtos;

public class CurrentResponseDto
{
    [JsonPropertyName("location")]
    public LocationDto Location { get; set; } = new();

    [JsonPropertyName("current")]
    public CurrentDataDto Current { get; set; } = new();
}