namespace AvaWeather.Services;

public sealed record IpLocation(string City, string Region, string Country, double Latitude, double Longitude);

public interface IIpLocationClient
{
    Task<IpLocation> LocateAsync(CancellationToken cancellationToken = default);
}
