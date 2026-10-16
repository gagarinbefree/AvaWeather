using Android.Content;
using Android.Locations;
using Android.OS;
using System.Diagnostics;
using Application.Interfaces;

namespace AvaWeather.Android;

public sealed class AndroidDeviceLocationClient(IApiAccessLog? log = null) : IDeviceLocationClient
{
    public async Task<DeviceCoordinates?> LocateAsync(CancellationToken cancellationToken = default)
    {
        var timer = Stopwatch.StartNew();
        log?.Write(new ApiAccessEvent("AndroidLocation", "location", "Start", 0));
        var activity = MainActivity.Current;
        if (activity is null)
        {
            log?.Write(new ApiAccessEvent("AndroidLocation", "location", "Unavailable", timer.ElapsedMilliseconds));
            return null;
        }
        if (!await activity.EnsureLocationPermissionAsync(cancellationToken))
        {
            log?.Write(new ApiAccessEvent("AndroidLocation", "location", "PermissionDenied", timer.ElapsedMilliseconds));
            return null;
        }

        var manager = activity.GetSystemService(Context.LocationService) as LocationManager;
        if (manager is null)
        {
            log?.Write(new ApiAccessEvent("AndroidLocation", "location", "Unavailable", timer.ElapsedMilliseconds));
            return null;
        }

        var providers = new List<string>();
        try
        {
            if (manager.IsProviderEnabled(LocationManager.NetworkProvider))
                providers.Add(LocationManager.NetworkProvider);
            if (activity.HasFineLocationPermission && manager.IsProviderEnabled(LocationManager.GpsProvider))
                providers.Add(LocationManager.GpsProvider);
            if (providers.Count == 0)
            {
                log?.Write(new ApiAccessEvent("AndroidLocation", "location", "Unavailable", timer.ElapsedMilliseconds));
                return null;
            }

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
                    log?.Write(new ApiAccessEvent("AndroidLocation", "location", "Success", timer.ElapsedMilliseconds));
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
                        {
                            log?.Write(new ApiAccessEvent("AndroidLocation", "location", "Success", timer.ElapsedMilliseconds));
                            return new DeviceCoordinates(lastKnown.Latitude, lastKnown.Longitude);
                        }
                    }
                    log?.Write(new ApiAccessEvent("AndroidLocation", "location", "Timeout", timer.ElapsedMilliseconds));
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
            log?.Write(new ApiAccessEvent("AndroidLocation", "location", "PermissionDenied", timer.ElapsedMilliseconds));
            return null;
        }
        catch (Java.Lang.IllegalArgumentException)
        {
            log?.Write(new ApiAccessEvent("AndroidLocation", "location", "Unavailable", timer.ElapsedMilliseconds));
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
