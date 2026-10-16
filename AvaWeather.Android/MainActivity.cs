using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia.Android;

namespace AvaWeather.Android;

[Activity(
    Label = "AvaWeather",
    Theme = "@style/AvaWeatherTheme",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public sealed class MainActivity : AvaloniaMainActivity
{
    private const int LocationPermissionRequestCode = 7101;
    private readonly SemaphoreSlim _permissionGate = new(1, 1);
    private TaskCompletionSource<bool>? _permissionRequest;

    internal static MainActivity? Current { get; private set; }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        Current = this;
        base.OnCreate(savedInstanceState);
    }

    protected override void OnDestroy()
    {
        if (ReferenceEquals(Current, this)) Current = null;
        _permissionRequest?.TrySetResult(false);
        base.OnDestroy();
    }

    internal bool HasCoarseLocationPermission =>
        CheckSelfPermission(global::Android.Manifest.Permission.AccessCoarseLocation) == Permission.Granted;

    internal bool HasFineLocationPermission =>
        CheckSelfPermission(global::Android.Manifest.Permission.AccessFineLocation) == Permission.Granted;

    internal async Task<bool> EnsureLocationPermissionAsync(CancellationToken cancellationToken)
    {
        await _permissionGate.WaitAsync(cancellationToken);
        try
        {
            if (HasCoarseLocationPermission || HasFineLocationPermission) return true;

            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            RunOnUiThread(() =>
            {
                if (HasCoarseLocationPermission || HasFineLocationPermission)
                    completion.TrySetResult(true);
                else
                {
                    _permissionRequest = completion;
                    RequestPermissions(
                        [global::Android.Manifest.Permission.AccessCoarseLocation,
                         global::Android.Manifest.Permission.AccessFineLocation],
                        LocationPermissionRequestCode);
                }
            });
            return await completion.Task.WaitAsync(cancellationToken);
        }
        finally
        {
            _permissionGate.Release();
        }
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions,
        Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        if (requestCode != LocationPermissionRequestCode) return;
        _permissionRequest?.TrySetResult(HasCoarseLocationPermission || HasFineLocationPermission);
        _permissionRequest = null;
    }
}
