// COM factory pattern adapted from Microsoft's WindowsAppSDK-Samples (MIT license).
// https://github.com/microsoft/WindowsAppSDK-Samples/tree/main/Samples/Widgets/cs-console-packaged/WidgetHelper
using System.Runtime.InteropServices;
using Microsoft.Windows.Widgets.Providers;
using WinRT;

namespace AvaWeather.Widget.Windows;

internal sealed class ComRegistration(uint cookie) : IDisposable
{
    [DllImport("ole32.dll")]
    private static extern int CoRegisterClassObject(
        [MarshalAs(UnmanagedType.LPStruct)] Guid clsid,
        [MarshalAs(UnmanagedType.IUnknown)] object factory,
        uint context, uint flags, out uint cookie);

    [DllImport("ole32.dll")]
    private static extern int CoRevokeClassObject(uint cookie);

    public static ComRegistration Register<T>() where T : IWidgetProvider, new()
    {
        var result = CoRegisterClassObject(typeof(T).GUID, new WidgetClassFactory<T>(), 4, 1, out var cookie);
        Marshal.ThrowExceptionForHR(result);
        return new ComRegistration(cookie);
    }

    public void Dispose() => CoRevokeClassObject(cookie);

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("00000001-0000-0000-C000-000000000046")]
    private interface IClassFactory
    {
        [PreserveSig] int CreateInstance(IntPtr outer, ref Guid interfaceId, out IntPtr instance);
        [PreserveSig] int LockServer(bool locked);
    }

    private sealed class WidgetClassFactory<T> : IClassFactory where T : IWidgetProvider, new()
    {
        public int CreateInstance(IntPtr outer, ref Guid interfaceId, out IntPtr instance)
        {
            instance = IntPtr.Zero;
            if (outer != IntPtr.Zero) return unchecked((int)0x80040110);
            if (interfaceId != typeof(T).GUID && interfaceId != new Guid("00000000-0000-0000-C000-000000000046"))
                return unchecked((int)0x80004002);
            instance = MarshalInspectable<IWidgetProvider>.FromManaged(new T());
            return 0;
        }

        public int LockServer(bool locked) => 0;
    }
}
