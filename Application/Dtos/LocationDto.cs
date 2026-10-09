using System.Text.Json.Serialization;

namespace Application.Dtos;

public class LocationDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("region")]
    public string Region { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;

    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lon")]
    public double Lon { get; set; }

    [JsonPropertyName("tz_id")]
    public string TzId { get; set; } = string.Empty;

    [JsonPropertyName("localtime")]
    public string LocalTime { get; set; } = string.Empty;
}