using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Localization;

if (args.Length != 1)
    throw new ArgumentException(StringLocalizer.Current.Get("KeyEmbedderOutputRequired"));

var apiKey = Environment.GetEnvironmentVariable("WEATHER_API_KEY");
if (string.IsNullOrWhiteSpace(apiKey))
    throw new InvalidOperationException(StringLocalizer.Current.Get("KeyEmbedderKeyMissing"));
var telemetryDsn = Environment.GetEnvironmentVariable("HAWK_DSN");
var openWeatherKey = Environment.GetEnvironmentVariable("OPENWEATHERMAP_API_KEY");

var key = RandomNumberGenerator.GetBytes(32);
var nonce = RandomNumberGenerator.GetBytes(12);
var plaintext = Encoding.UTF8.GetBytes(apiKey);
var ciphertext = new byte[plaintext.Length];
var tag = new byte[16];
using (var aes = new AesGcm(key, tag.Length))
    aes.Encrypt(nonce, plaintext, ciphertext, tag);
CryptographicOperations.ZeroMemory(plaintext);

var secondaryKey = RandomNumberGenerator.GetBytes(32);
var secondaryNonce = RandomNumberGenerator.GetBytes(12);
var secondaryPlaintext = Encoding.UTF8.GetBytes(openWeatherKey ?? string.Empty);
var secondaryCiphertext = new byte[secondaryPlaintext.Length];
var secondaryTag = new byte[16];
using (var aes = new AesGcm(secondaryKey, secondaryTag.Length))
    aes.Encrypt(secondaryNonce, secondaryPlaintext, secondaryCiphertext, secondaryTag);
CryptographicOperations.ZeroMemory(secondaryPlaintext);

var source = $$"""
    // Generated for a release build. The decryption material is in the executable.
    #nullable enable
    namespace AvaWeather.Services;

    public static partial class EmbeddedWeatherApiKey
    {
        static partial void Populate(ref byte[]? key, ref byte[]? nonce, ref byte[]? ciphertext, ref byte[]? tag)
        {
            key = Convert.FromBase64String("{{Convert.ToBase64String(key)}}");
            nonce = Convert.FromBase64String("{{Convert.ToBase64String(nonce)}}");
            ciphertext = Convert.FromBase64String("{{Convert.ToBase64String(ciphertext)}}");
            tag = Convert.FromBase64String("{{Convert.ToBase64String(tag)}}");
        }
    }

    public static partial class EmbeddedWeatherTelemetryDsn
    {
        static partial void Populate(ref string? dsn)
        {
            dsn = {{JsonSerializer.Serialize(telemetryDsn)}};
        }
    }

    public static partial class EmbeddedOpenWeatherMapKey
    {
        static partial void Populate(ref byte[]? key, ref byte[]? nonce, ref byte[]? ciphertext, ref byte[]? tag)
        {
            key = Convert.FromBase64String("{{Convert.ToBase64String(secondaryKey)}}");
            nonce = Convert.FromBase64String("{{Convert.ToBase64String(secondaryNonce)}}");
            ciphertext = Convert.FromBase64String("{{Convert.ToBase64String(secondaryCiphertext)}}");
            tag = Convert.FromBase64String("{{Convert.ToBase64String(secondaryTag)}}");
        }
    }
    """;

var output = Path.GetFullPath(args[0]);
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
File.WriteAllText(output, source);
CryptographicOperations.ZeroMemory(key);
CryptographicOperations.ZeroMemory(secondaryKey);
Console.WriteLine(StringLocalizer.Current.Get("KeyEmbedderGenerated"));
