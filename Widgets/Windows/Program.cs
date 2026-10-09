using System.Runtime.InteropServices;
using Microsoft.Windows.Widgets.Providers;

namespace AvaWeather.Widget.Windows;

internal static class Program
{
    [MTAThread]
    private static void Main(string[] args)
    {
        if (args.Length != 1 || args[0] != "-RegisterProcessAsComServer") return;

        WinRT.ComWrappersSupport.InitializeComWrappers();
        using var registration = ComRegistration.Register<WeatherWidgetProvider>();
        using var stop = new ManualResetEvent(false);
        Console.CancelKeyPress += (_, eventArgs) => { eventArgs.Cancel = true; stop.Set(); };
        stop.WaitOne();
    }
}
