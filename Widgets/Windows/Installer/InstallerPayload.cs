using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace AvaWeather.Widget.Installer;

public static class InstallerPayload
{
    public static X509Certificate2 Verify(byte[] package, byte[] certificate,
        string expectedPackageSha256, string expectedCertificateSha256)
    {
        if (package.Length == 0 || certificate.Length == 0 ||
            !HashMatches(package, expectedPackageSha256) ||
            !HashMatches(certificate, expectedCertificateSha256))
            throw new InvalidDataException("Installer payload integrity check failed.");

        X509Certificate2 signer;
        try { signer = X509CertificateLoader.LoadCertificate(certificate); }
        catch (CryptographicException error)
        {
            throw new InvalidDataException("Installer certificate is invalid.", error);
        }
        if (signer.Subject != "CN=AvaWeather Development")
        {
            signer.Dispose();
            throw new InvalidDataException("Unexpected package publisher certificate.");
        }
        return signer;
    }

    private static bool HashMatches(byte[] data, string expected) =>
        string.Equals(Convert.ToHexString(SHA256.HashData(data)), expected,
            StringComparison.OrdinalIgnoreCase);
}
