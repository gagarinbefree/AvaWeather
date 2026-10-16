using Application.Interfaces;
using Application.Services;

namespace Tests;

public class DeviceFirstLocationClientTests
{
    [Fact]
    public async Task Uses_device_coordinates_without_requesting_ip_location()
    {
        var fallback = new StubIpLocationClient();
        var location = await new DeviceFirstLocationClient(
            new StubDeviceLocationClient(new DeviceCoordinates(58.0047, 56.2514)), fallback)
            .LocateAsync(TestContext.Current.CancellationToken);

        Assert.Equal(58.0047, location.Latitude);
        Assert.Equal(56.2514, location.Longitude);
        Assert.Equal(string.Empty, location.Country);
        Assert.Equal(0, fallback.Calls);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(91d, 56d)]
    [InlineData(0d, 0d)]
    public async Task Falls_back_to_ip_when_device_location_is_missing_or_invalid(double? latitude, double? longitude)
    {
        var fallback = new StubIpLocationClient();
        DeviceCoordinates? coordinates = latitude is null ? null : new(latitude.Value, longitude!.Value);

        var location = await new DeviceFirstLocationClient(
            new StubDeviceLocationClient(coordinates), fallback)
            .LocateAsync(TestContext.Current.CancellationToken);

        Assert.Equal("IP city", location.City);
        Assert.Equal(1, fallback.Calls);
    }

    [Fact]
    public async Task Forwards_cancellation_to_device_provider()
    {
        using var source = new CancellationTokenSource();
        var device = new StubDeviceLocationClient(null);
        await new DeviceFirstLocationClient(device, new StubIpLocationClient()).LocateAsync(source.Token);
        Assert.Equal(source.Token, device.SeenToken);
    }

    private sealed class StubDeviceLocationClient(DeviceCoordinates? coordinates) : IDeviceLocationClient
    {
        public CancellationToken SeenToken { get; private set; }
        public Task<DeviceCoordinates?> LocateAsync(CancellationToken cancellationToken = default)
        {
            SeenToken = cancellationToken;
            return Task.FromResult(coordinates);
        }
    }

    private sealed class StubIpLocationClient : IIpLocationClient
    {
        public int Calls { get; private set; }
        public Task<IpLocation> LocateAsync(CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(new IpLocation("IP city", "", "Germany", 52.5, 13.4));
        }
    }
}
