using System.Text.Json.Serialization;

namespace Application.Dtos;

public class AstroDto
{
    [JsonPropertyName("sunrise")]
    public string Sunrise { get; set; } = string.Empty;

    [JsonPropertyName("sunset")]
    public string Sunset { get; set; } = string.Empty;
}