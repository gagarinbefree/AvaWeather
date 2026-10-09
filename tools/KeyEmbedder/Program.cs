using System.Security.Cryptography;
using System.Text;

if (args.Length != 1)
    throw new ArgumentException("Provide the generated C# output path.");

var apiKey = Environment.GetEnvironmentVariable("WEATHER_API_KEY");
if (string.IsNullOrWhiteSpace(apiKey))
    throw new InvalidOperationException("WEATHER_API_KEY is missing or empty.");

var key = RandomNumberGenerator.GetBytes(32);
var nonce = RandomNumberGenerator.GetBytes(12);
var plaintext = Encoding.UTF8.GetBytes(apiKey);
var ciphertext = new byte[plaintext.Length];
var tag = new byte[16];
using (var aes = new AesGcm(key, tag.Length))
    aes.Encrypt(nonce, plaintext, ciphertext, tag);
CryptographicOperations.ZeroMemory(plaintext);

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
    """;

var output = Path.GetFullPath(args[0]);
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
File.WriteAllText(output, source);
CryptographicOperations.ZeroMemory(key);
Console.WriteLine("Embedded key source generated.");
