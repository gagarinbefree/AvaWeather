using System.Text.Json.Serialization;

namespace Application.Dtos;

public class ForecastDayDto
{
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("day")]
    public DayDto Day { get; set; } = new();

    [JsonPropertyName("astro")]
    public AstroDto Astro { get; set; } = new();

    [JsonPropertyName("hour")]
    public List<HourDto> Hour { get; set; } = new();
}