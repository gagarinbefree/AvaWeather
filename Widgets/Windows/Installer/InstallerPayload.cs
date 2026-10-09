using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Weather.Localization;

namespace AvaWeather.Widget.Installer;

public static class InstallerPayload
{
    public static X509Certificate2 Verify(byte[] package, byte[] certificate,
        string expectedPackageSha256, string expectedCertificateSha256)
    {
        if (package.Length == 0 || certificate.Length == 0 ||
            !HashMatches(package, expectedPackageSha256) ||
            !HashMatches(certificate, expectedCertificateSha256))
            throw new InvalidDataException(StringLocalizer.Current.Get("InstallerPayloadIntegrityFailed"));

        X509Certificate2 signer;
        try { signer = X509CertificateLoader.LoadCertificate(certificate); }
        catch (CryptographicException error)
        {
            throw new InvalidDataException(StringLocalizer.Current.Get("InstallerInvalidCertificate"), error);
        }
        if (signer.Subject != "CN=AvaWeather Development")
        {
            signer.Dispose();
            throw new InvalidDataException(StringLocalizer.Current.Get("InstallerUnexpectedPublisher"));
        }
        return signer;
    }

    private static bool HashMatches(byte[] data, string expected) =>
        string.Equals(Convert.ToHexString(SHA256.HashData(data)), expected,
            StringComparison.OrdinalIgnoreCase);
}
