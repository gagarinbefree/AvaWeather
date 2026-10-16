using System.Globalization;
using System.Runtime.InteropServices.JavaScript;
using Application.Interfaces;

namespace AvaWeather.Browser;

public sealed partial class BrowserDeviceLocationClient : IDeviceLocationClient
{
    [JSImport("getCurrentPosition", "deviceLocation")]
    private static partial Task<string?> GetCurrentPositionAsync();

    public async Task<DeviceCoordinates?> LocateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await GetCurrentPositionAsync().WaitAsync(cancellationToken);
            var parts = value?.Split(',');
            return parts is { Length: 2 } &&
                   double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude) &&
                   double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude)
                ? new DeviceCoordinates(latitude, longitude) : null;
        }
        catch (JSException)
        {
            return null;
        }
    }
}
