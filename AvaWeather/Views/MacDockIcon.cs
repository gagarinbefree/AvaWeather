using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Platform;

namespace AvaWeather.Views;

public static class MacDockIcon
{
    private const string ObjectiveC = "/usr/lib/libobjc.A.dylib";

    public static bool Apply()
    {
        if (!OperatingSystem.IsMacOS()) return false;

        try
        {
            // The PNG is an Avalonia resource inside the single-file executable.
            using var source = AssetLoader.Open(new Uri("avares://AvaWeather/Assets/app-icon.png"));
            using var buffer = new MemoryStream();
            source.CopyTo(buffer);
            var png = buffer.ToArray();

            NativeLibrary.Load("/System/Library/Frameworks/AppKit.framework/AppKit");
            var data = SendBytes(GetClass("NSData"), Selector("dataWithBytes:length:"), png, (nuint)png.Length);
            if (data == 0) return false;

            var image = Send(Send(GetClass("NSImage"), Selector("alloc")), Selector("initWithData:"), data);
            if (image == 0) return false;

            try
            {
                var application = Send(GetClass("NSApplication"), Selector("sharedApplication"));
                if (application == 0) return false;

                SendVoid(application, Selector("setApplicationIconImage:"), image);
                return Send(application, Selector("applicationIconImage")) == image;
            }
            finally
            {
                SendVoid(image, Selector("release"));
            }
        }
        catch (Exception exception)
        {
            Trace.WriteLine($"Could not set the macOS Dock icon: {exception}");
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
