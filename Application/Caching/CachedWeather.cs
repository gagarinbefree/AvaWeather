using Domain.Entities;

namespace Application.Caching;

public sealed record CachedWeather(int Version, DateTimeOffset FetchedAtUtc, string Language, WeatherData Weather);
