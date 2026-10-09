using System.Text.Json;
using AvaWeather.Widget.Windows;
using AvaWeather.Widgets;

namespace AvaWeather.Tests;

public class WidgetCardTests
{
    [Fact]
    public void Adaptive_card_escapes_city_and_contains_live_values()
    {
        var snapshot = new WidgetSnapshot("Saint \"Petersburg\"", "9°", "Feels like 5°",
            "Rain", "Humidity 70%", "#D7EAF5", "#255B78", "cloud.rain.fill");

        using var card = JsonDocument.Parse(WidgetCard.Render(snapshot));
        var root = card.RootElement;
        Assert.Equal("AdaptiveCard", root.GetProperty("type").GetString());
        var body = root.GetProperty("body");
        Assert.Equal("Saint \"Petersburg\"", body[0].GetProperty("text").GetString());
        Assert.Equal("9°", body[1].GetProperty("text").GetString());
        Assert.Equal("Humidity 70%", body[4].GetProperty("text").GetString());
        var background = root.GetProperty("backgroundImage").GetString();
        Assert.StartsWith("data:image/png;base64,", background);
        var png = Convert.FromBase64String(background!["data:image/png;base64,".Length..]);
        Assert.Equal(new byte[] { 137, 80, 78, 71 }, png[..4]);
    }
}
