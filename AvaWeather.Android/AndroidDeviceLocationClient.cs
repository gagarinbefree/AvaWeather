using Android.Content;
using Android.Locations;
using Android.OS;
using Application.Interfaces;

namespace AvaWeather.Android;

public sealed class AndroidDeviceLocationClient : IDeviceLocationClient
{
    public async Task<DeviceCoordinates?> LocateAsync(CancellationToken cancellationToken = default)
    {
        var activity = MainActivity.Current;
        if (activity is null || !await activity.EnsureLocationPermissionAsync(cancellationToken)) return null;

        var manager = activity.GetSystemService(Context.LocationService) as LocationManager;
        if (manager is null) return null;

        var providers = new List<string>();
        try
        {
            if (manager.IsProviderEnabled(LocationManager.NetworkProvider))
                providers.Add(LocationManager.NetworkProvider);
            if (activity.HasFineLocationPermission && manager.IsProviderEnabled(LocationManager.GpsProvider))
                providers.Add(LocationManager.GpsProvider);
            if (providers.Count == 0) return null;

            var completion = new TaskCompletionSource<Location>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var listener = new FirstLocationListener(location => completion.TrySetResult(location));
            try
            {
                foreach (var provider in providers)
                    manager.RequestLocationUpdates(provider, 0L, 0f, listener, Looper.MainLooper);

                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(8));
                try
                {
                    var location = await completion.Task.WaitAsync(timeout.Token);
                    return new DeviceCoordinates(location.Latitude, location.Longitude);
                }
                catch (System.OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    // A recent device fix is still preferable to IP location.
                    foreach (var provider in providers)
                    {
                        var lastKnown = manager.GetLastKnownLocation(provider);
                        if (lastKnown is null) continue;
                        var age = DateTimeOffset.UtcNow -
                            DateTimeOffset.FromUnixTimeMilliseconds(lastKnown.Time);
                        if (age >= TimeSpan.Zero && age < TimeSpan.FromMinutes(10))
                            return new DeviceCoordinates(lastKnown.Latitude, lastKnown.Longitude);
                    }
                    return null;
                }
            }
            finally
            {
                manager.RemoveUpdates(listener);
            }
        }
        catch (Java.Lang.SecurityException)
        {
            return null;
        }
        catch (Java.Lang.IllegalArgumentException)
        {
            return null;
        }
    }

    private sealed class FirstLocationListener(Action<Location> onLocation) : Java.Lang.Object, ILocationListener
    {
        public void OnLocationChanged(Location location) => onLocation(location);
        public void OnProviderDisabled(string provider) { }
        public void OnProviderEnabled(string provider) { }
        public void OnStatusChanged(string? provider, Availability status, Bundle? extras) { }
    }
}
