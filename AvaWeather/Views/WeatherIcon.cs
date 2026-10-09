using System.Collections.Concurrent;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using AvaWeather.ViewModels;

namespace AvaWeather.Views;

public sealed class WeatherIcon : Grid
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(8) };
    private static readonly ConcurrentDictionary<string, Task<Bitmap?>> Cache = new();
    private readonly Image _image = new() { Stretch = Stretch.Uniform, IsVisible = false };
    private readonly Avalonia.Controls.Shapes.Path _fallback;

    public static readonly StyledProperty<string?> UrlProperty =
        AvaloniaProperty.Register<WeatherIcon, string?>(nameof(Url));
    public static readonly StyledProperty<WeatherConditionKind> KindProperty =
        AvaloniaProperty.Register<WeatherIcon, WeatherConditionKind>(nameof(Kind));

    public string? Url { get => GetValue(UrlProperty); set => SetValue(UrlProperty, value); }
    public WeatherConditionKind Kind { get => GetValue(KindProperty); set => SetValue(KindProperty, value); }

    public WeatherIcon(double size = 32, IBrush? fallbackBrush = null)
    {
        Width = size;
        Height = size;
        _fallback = VectorIcons.CreateWeather(WeatherConditionKind.PartlyCloudy, size, fallbackBrush ?? Brushes.SteelBlue);
        Children.Add(_fallback);
        Children.Add(_image);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == KindProperty && _fallback is not null)
            _fallback.Data = VectorIcons.ForWeather(Kind);
        if (change.Property == UrlProperty && _image is not null)
            _ = ShowImageAsync(Url);
    }

    private async Task ShowImageAsync(string? url)
    {
        _image.IsVisible = false;
        _fallback.IsVisible = true;
        if (!TryWeatherApiUrl(url, out var safeUrl)) return;

        var bitmap = await Cache.GetOrAdd(safeUrl, DownloadAsync);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (Url != url || bitmap is null) return;
            _image.Source = bitmap;
            _image.IsVisible = true;
            _fallback.IsVisible = false;
        });
    }

    private static bool TryWeatherApiUrl(string? url, out string safeUrl)
    {
        var normalized = url?.StartsWith("//", StringComparison.Ordinal) == true ? "https:" + url : url;
        if (Uri.TryCreate(normalized, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps &&
            uri.Host.Equals("cdn.weatherapi.com", StringComparison.OrdinalIgnoreCase))
        {
            safeUrl = uri.AbsoluteUri;
            return true;
        }
        safeUrl = string.Empty;
        return false;
    }

    private static async Task<Bitmap?> DownloadAsync(string url)
    {
        try
        {
            var bytes = await Client.GetByteArrayAsync(url);
            using var stream = new MemoryStream(bytes);
            return new Bitmap(stream);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
