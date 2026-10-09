namespace AvaWeather.Services;

public sealed record LocalizedPlace(string City, string? Region);

public interface IPlaceNameLocalizer
{
    Task<LocalizedPlace?> ResolveRussianAsync(
        string city, string country, double latitude, double longitude,
        CancellationToken cancellationToken = default);
}
