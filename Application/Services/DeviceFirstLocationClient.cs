using Application.Interfaces;

namespace Application.Services;

public sealed class DeviceFirstLocationClient(
    IDeviceLocationClient device, IIpLocationClient ipLocation) : IIpLocationClient
{
    public async Task<IpLocation> LocateAsync(CancellationToken cancellationToken = default)
    {
        var coordinates = await device.LocateAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (coordinates is { } found &&
            double.IsFinite(found.Latitude) && double.IsFinite(found.Longitude) &&
            found.Latitude is >= -90 and <= 90 && found.Longitude is >= -180 and <= 180 &&
            (found.Latitude != 0 || found.Longitude != 0))
        {
            // WeatherAPI supplies the place name and country for these coordinates.
            return new IpLocation(string.Empty, string.Empty, string.Empty,
                found.Latitude, found.Longitude);
        }

        return await ipLocation.LocateAsync(cancellationToken);
    }
}
