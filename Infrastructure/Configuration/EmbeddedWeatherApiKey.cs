using System.Security.Cryptography;
using System.Text;

namespace AvaWeather.Services;

public static partial class EmbeddedWeatherApiKey
{
    public static string? Read()
    {
        byte[]? key = null, nonce = null, ciphertext = null, tag = null;
        Populate(ref key, ref nonce, ref ciphertext, ref tag);
        if (key is null || nonce is null || ciphertext is null || tag is null) return null;

        var plaintext = new byte[ciphertext.Length];
        try
        {
            using var aes = new AesGcm(key, tag.Length);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
            return Encoding.UTF8.GetString(plaintext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    static partial void Populate(ref byte[]? key, ref byte[]? nonce, ref byte[]? ciphertext, ref byte[]? tag);
}
