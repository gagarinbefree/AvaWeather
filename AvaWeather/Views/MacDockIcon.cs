using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Platform;

namespace AvaWeather.Views;

public static class MacDockIcon
{
    private const string ObjectiveC = "/usr/lib/libobjc.A.dylib";

    public static bool Apply() => Apply(out _);

    public static bool Apply(out string? error)
    {
        error = null;
        if (!OperatingSystem.IsMacOS())
        {
            error = "This platform is not macOS.";
            return false;
        }

        try
        {
            // The PNG is an Avalonia resource inside the single-file executable.
            using var source = AssetLoader.Open(new Uri("avares://AvaWeather/Assets/app-icon.png"));
            using var buffer = new MemoryStream();
            source.CopyTo(buffer);
            var png = buffer.ToArray();

            NativeLibrary.Load("/System/Library/Frameworks/AppKit.framework/AppKit");
            var data = SendBytes(GetClass("NSData"), Selector("dataWithBytes:length:"), png, (nuint)png.Length);
            if (data == 0)
            {
                error = "AppKit did not create NSData from the embedded PNG.";
                return false;
            }

            var image = Send(Send(GetClass("NSImage"), Selector("alloc")), Selector("initWithData:"), data);
            if (image == 0)
            {
                error = "AppKit did not create NSImage from the embedded PNG.";
                return false;
            }

            try
            {
                var application = Send(GetClass("NSApplication"), Selector("sharedApplication"));
                if (application == 0)
                {
                    error = "AppKit did not return a shared NSApplication.";
                    return false;
                }

                SendVoid(application, Selector("setApplicationIconImage:"), image);
                if (Send(application, Selector("applicationIconImage")) == 0)
                {
                    error = "NSApplication did not expose an icon image after setting it.";
                    return false;
                }
                return true;
            }
            finally
            {
                SendVoid(image, Selector("release"));
            }
        }
        catch (Exception exception)
        {
            error = exception.ToString();
            Trace.WriteLine($"Could not set the macOS Dock icon: {error}");
            return false;
        }
    }

    private static nint Selector(string name) => RegisterSelector(name);

    [DllImport(ObjectiveC, EntryPoint = "objc_getClass", CharSet = CharSet.Ansi)]
    private static extern nint GetClass(string name);

    [DllImport(ObjectiveC, EntryPoint = "sel_registerName", CharSet = CharSet.Ansi)]
    private static extern nint RegisterSelector(string name);

    [DllImport(ObjectiveC, EntryPoint = "objc_msgSend")]
    private static extern nint Send(nint receiver, nint selector);

    [DllImport(ObjectiveC, EntryPoint = "objc_msgSend")]
    private static extern nint Send(nint receiver, nint selector, nint value);

    [DllImport(ObjectiveC, EntryPoint = "objc_msgSend")]
    private static extern nint SendBytes(nint receiver, nint selector, byte[] bytes, nuint length);

    [DllImport(ObjectiveC, EntryPoint = "objc_msgSend")]
    private static extern void SendVoid(nint receiver, nint selector);

    [DllImport(ObjectiveC, EntryPoint = "objc_msgSend")]
    private static extern void SendVoid(nint receiver, nint selector, nint value);
}
