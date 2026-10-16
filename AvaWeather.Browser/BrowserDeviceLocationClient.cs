using System.Globalization;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using Application.Interfaces;

namespace AvaWeather.Browser;

public sealed partial class BrowserDeviceLocationClient(IApiAccessLog? log = null) : IDeviceLocationClient
{
    [JSImport("getCurrentPosition", "deviceLocation")]
    private static partial Task<string?> GetCurrentPositionAsync();

    public async Task<DeviceCoordinates?> LocateAsync(CancellationToken cancellationToken = default)
    {
        var timer = Stopwatch.StartNew();
        log?.Write(new ApiAccessEvent("BrowserLocation", "location", "Start", 0));
        try
        {
            var value = await GetCurrentPositionAsync().WaitAsync(cancellationToken);
            var parts = value?.Split(',');
            var coordinates = parts is { Length: 2 } &&
                   double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude) &&
                   double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude)
                ? new DeviceCoordinates(latitude, longitude) : null;
            log?.Write(new ApiAccessEvent("BrowserLocation", "location",
                coordinates is null ? "Unavailable" : "Success", timer.ElapsedMilliseconds));
            return coordinates;
        }
        catch (JSException)
        {
            log?.Write(new ApiAccessEvent("BrowserLocation", "location", "TransportError",
                timer.ElapsedMilliseconds, ErrorType: "JSException"));
            return null;
        }
        catch (OperationCanceledException)
        {
            log?.Write(new ApiAccessEvent("BrowserLocation", "location", "Canceled", timer.ElapsedMilliseconds));
            throw;
        }
    }
}
