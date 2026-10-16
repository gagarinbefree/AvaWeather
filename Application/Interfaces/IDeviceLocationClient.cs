namespace Application.Interfaces;

public sealed record DeviceCoordinates(double Latitude, double Longitude);

public interface IDeviceLocationClient
{
    Task<DeviceCoordinates?> LocateAsync(CancellationToken cancellationToken = default);
}
