using Avalonia.Layout;
using Avalonia.Media;
using AvaWeather.ViewModels;
using ShapePath = Avalonia.Controls.Shapes.Path;

namespace AvaWeather.Views;

internal enum VectorIconKind
{
    Sun, Location, Clock, Thermometer, Droplet, Wind, Gauge, Eye,
    Calendar, Refresh, Alert, Sunrise
}

internal static class VectorIcons
{
    private static readonly Geometry Sun = Geometry.Parse("M12,8 A4,4 0 1 0 12,16 A4,4 0 1 0 12,8 M12,2 L12,4 M12,20 L12,22 M4.93,4.93 L6.34,6.34 M17.66,17.66 L19.07,19.07 M2,12 L4,12 M20,12 L22,12 M4.93,19.07 L6.34,17.66 M17.66,6.34 L19.07,4.93");
    private static readonly Geometry Cloud = Geometry.Parse("M6,18 L18,18 C20,18 21,16.5 21,15 C21,13 19.5,11.5 17.5,11.5 C16.5,11.5 15.7,11.8 15,12.4 C14.7,9.4 12.2,7 9.2,7 C5.8,7 3,9.8 3,13.2 C3,15.4 4.1,17 6,18 Z");

    public static ShapePath Create(VectorIconKind kind, double size, IBrush stroke) => Shape(For(kind), size, stroke);

    public static ShapePath CreateWeather(WeatherConditionKind kind, double size, IBrush stroke) =>
        Shape(ForWeather(kind), size, stroke);

    public static Geometry ForWeather(WeatherConditionKind kind) => kind switch
    {
        WeatherConditionKind.ClearDay => Sun,
        WeatherConditionKind.ClearNight => Geometry.Parse("M20,15.5 A8,8 0 0 1 8.5,4 A8.5,8.5 0 1 0 20,15.5 Z"),
        WeatherConditionKind.Cloudy => Cloud,
        WeatherConditionKind.PartlyCloudy => Geometry.Parse("M8,3 L8,5 M3,8 L5,8 M4.5,4.5 L6,6 M8,5 A3,3 0 0 1 11,8 M6,18 L18,18 C20,18 21,16.5 21,15 C21,13 19.5,11.5 17.5,11.5 C16.5,11.5 15.7,11.8 15,12.4 C14.7,9.4 12.2,7 9.2,7 C5.8,7 3,9.8 3,13.2 C3,15.4 4.1,17 6,18 Z"),
        WeatherConditionKind.Fog => Geometry.Parse("M3,8 L21,8 M5,12 L19,12 M3,16 L21,16 M7,20 L17,20"),
        WeatherConditionKind.Rain => Geometry.Parse("M6,15 L18,15 C20,15 21,13.5 21,12 C21,10 19.5,8.5 17.5,8.5 C16.5,8.5 15.7,8.8 15,9.4 C14.7,6.4 12.2,4 9.2,4 C5.8,4 3,6.8 3,10.2 C3,12.4 4.1,14 6,15 Z M7,18 L6,21 M12,18 L11,21 M17,18 L16,21"),
        WeatherConditionKind.Snow => Geometry.Parse("M6,14 L18,14 C20,14 21,12.5 21,11 C21,9 19.5,7.5 17.5,7.5 C16.5,7.5 15.7,7.8 15,8.4 C14.7,5.4 12.2,3 9.2,3 C5.8,3 3,5.8 3,9.2 C3,11.4 4.1,13 6,14 Z M12,16 L12,22 M9,19 L15,19 M9.8,16.8 L14.2,21.2 M14.2,16.8 L9.8,21.2"),
        WeatherConditionKind.Thunder => Geometry.Parse("M6,14 L18,14 C20,14 21,12.5 21,11 C21,9 19.5,7.5 17.5,7.5 C16.5,7.5 15.7,7.8 15,8.4 C14.7,5.4 12.2,3 9.2,3 C5.8,3 3,5.8 3,9.2 C3,11.4 4.1,13 6,14 Z M13,15 L10,19 L13,19 L11,23"),
        _ => Cloud
    };

    private static Geometry For(VectorIconKind kind) => kind switch
    {
        VectorIconKind.Sun => Sun,
        VectorIconKind.Location => Geometry.Parse("M12,22 C12,22 5,15 5,10 A7,7 0 1 1 19,10 C19,15 12,22 12,22 Z M12,7 A3,3 0 1 0 12,13 A3,3 0 1 0 12,7"),
        VectorIconKind.Clock => Geometry.Parse("M12,2 A10,10 0 1 0 12,22 A10,10 0 1 0 12,2 M12,6 L12,12 L16,14"),
        VectorIconKind.Thermometer => Geometry.Parse("M10,14 L10,5 A2,2 0 0 1 14,5 L14,14 A4,4 0 1 1 10,14 Z M12,10 L12,18"),
        VectorIconKind.Droplet => Geometry.Parse("M12,2 C12,2 5,10 5,15 A7,7 0 1 0 19,15 C19,10 12,2 12,2 Z"),
        VectorIconKind.Wind => Geometry.Parse("M2,8 L16,8 C19,8 20,6.5 20,5 C20,3.5 19,3 18,3 C16.5,3 16,4 16,5 M2,12 L19,12 C21,12 22,13 22,14.5 C22,16 21,17 19.5,17 C18,17 17,16 17,15 M2,16 L11,16 C13,16 14,17 14,18.5 C14,20 13,21 11.5,21 C10,21 9,20 9,19"),
        VectorIconKind.Gauge => Geometry.Parse("M4,18 A9,9 0 1 1 20,18 L4,18 Z M12,16 L16,10 M8,19 L8,20 M16,19 L16,20"),
        VectorIconKind.Eye => Geometry.Parse("M2,12 C5,7 8,5 12,5 C16,5 19,7 22,12 C19,17 16,19 12,19 C8,19 5,17 2,12 Z M12,9 A3,3 0 1 0 12,15 A3,3 0 1 0 12,9"),
        VectorIconKind.Calendar => Geometry.Parse("M4,5 L20,5 L20,21 L4,21 Z M4,9 L20,9 M8,2 L8,7 M16,2 L16,7 M8,13 L10,13 M14,13 L16,13 M8,17 L10,17 M14,17 L16,17"),
        VectorIconKind.Refresh => Geometry.Parse("M20,7 L20,3 L16,3 M20,3 C17,1 12,1 9,3 C6,5 5,7 5,10 M4,17 L4,21 L8,21 M4,21 C7,23 12,23 15,21 C18,19 19,17 19,14"),
        VectorIconKind.Alert => Geometry.Parse("M12,3 L22,21 L2,21 Z M12,9 L12,14 M12,17 L12,18"),
        VectorIconKind.Sunrise => Geometry.Parse("M2,20 L22,20 M4,16 L20,16 M7,16 A5,5 0 0 1 17,16 M12,2 L12,6 M4.9,6.9 L7,9 M19.1,6.9 L17,9 M12,8 L12,10"),
        _ => Sun
    };

    private static ShapePath Shape(Geometry geometry, double size, IBrush stroke) => new()
    {
        Data = geometry,
        Width = size,
        Height = size,
        Stretch = Stretch.Uniform,
        Stroke = stroke,
        StrokeThickness = 1.8,
        StrokeLineCap = PenLineCap.Round,
        StrokeJoin = PenLineJoin.Round,
        VerticalAlignment = VerticalAlignment.Center,
        IsHitTestVisible = false
    };
}
