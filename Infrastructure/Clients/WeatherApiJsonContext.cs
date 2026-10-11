using System.Text.Json.Serialization;
using Application.Dtos;

namespace Infrastructure.Clients;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(CurrentResponseDto))]
[JsonSerializable(typeof(ForecastResponseDto))]
internal partial class WeatherApiJsonContext : JsonSerializerContext;
