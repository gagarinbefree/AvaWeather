using System.Text.Json;
using System.Buffers.Binary;
using System.IO.Compression;
using AvaWeather.Widgets;
using Weather.Localization;

namespace AvaWeather.Widget.Windows;

internal static class WidgetCard
{
    public static string Render(WidgetSnapshot snapshot)
    {
        var card = new
        {
            type = "AdaptiveCard",
            version = "1.5",
            backgroundImage = BackgroundImage(snapshot.Background),
            body = new object[]
            {
                new { type = "TextBlock", text = snapshot.City, size = "Medium", weight = "Bolder", wrap = true },
                new { type = "TextBlock", text = snapshot.Temperature, size = "ExtraLarge", weight = "Bolder", spacing = "Small" },
                new { type = "TextBlock", text = snapshot.Condition, size = "Medium", wrap = true },
                new { type = "TextBlock", text = snapshot.FeelsLike, spacing = "Medium" },
                new { type = "TextBlock", text = snapshot.Humidity, spacing = "Small" }
            }
        };
        return JsonSerializer.Serialize(card);
    }

    private static string BackgroundImage(string hex)
    {
        var rgb = Convert.FromHexString(hex.TrimStart('#'));
        if (rgb.Length != 3) throw new ArgumentException(StringLocalizer.Current.Get("ExpectedRgbColor"), nameof(hex));
        using var image = new MemoryStream();
        image.Write([137, 80, 78, 71, 13, 10, 26, 10]);
        byte[] header = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(header, 1);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4), 1);
        header[8] = 8;
        header[9] = 2;
        WriteChunk(image, "IHDR", header);
        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
            zlib.Write([0, rgb[0], rgb[1], rgb[2]]);
        WriteChunk(image, "IDAT", compressed.ToArray());
        WriteChunk(image, "IEND", []);
        return "data:image/png;base64," + Convert.ToBase64String(image.ToArray());
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        Span<byte> number = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(number, (uint)data.Length);
        stream.Write(number);
        var tag = System.Text.Encoding.ASCII.GetBytes(type);
        stream.Write(tag);
        stream.Write(data);
        uint crc = 0xFFFFFFFF;
        foreach (var value in tag.Concat(data))
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
                crc = (crc >> 1) ^ ((crc & 1) == 1 ? 0xEDB88320 : 0u);
        }
        BinaryPrimitives.WriteUInt32BigEndian(number, ~crc);
        stream.Write(number);
    }
}
