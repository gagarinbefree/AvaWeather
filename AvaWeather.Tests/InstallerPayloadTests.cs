using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using AvaWeather.Widget.Installer;

namespace AvaWeather.Tests;

public class InstallerPayloadTests
{
    [Fact]
    public void Matching_package_and_signer_certificate_are_accepted()
    {
        using var certificate = CreateCertificate();
        var package = "signed-msix-content"u8.ToArray();
        var publicCertificate = certificate.Export(X509ContentType.Cert);

        var result = InstallerPayload.Verify(package, publicCertificate,
            Sha256(package), Sha256(publicCertificate));

        Assert.Equal("CN=AvaWeather Development", result.Subject);
    }

    [Fact]
    public void Altered_package_or_certificate_is_rejected_before_trust()
    {
        using var certificate = CreateCertificate();
        var package = "signed-msix-content"u8.ToArray();
        var publicCertificate = certificate.Export(X509ContentType.Cert);

        Assert.Throws<InvalidDataException>(() => InstallerPayload.Verify(
            "altered"u8.ToArray(), publicCertificate, Sha256(package), Sha256(publicCertificate)));
        Assert.Throws<InvalidDataException>(() => InstallerPayload.Verify(
            package, publicCertificate, Sha256(package), Sha256("different"u8.ToArray())));
    }

    [Fact]
    public void Certificate_from_another_publisher_is_rejected()
    {
        using var certificate = CreateCertificate("CN=Other Publisher");
        var package = "signed-msix-content"u8.ToArray();
        var publicCertificate = certificate.Export(X509ContentType.Cert);

        Assert.Throws<InvalidDataException>(() => InstallerPayload.Verify(
            package, publicCertificate, Sha256(package), Sha256(publicCertificate)));
    }

    private static X509Certificate2 CreateCertificate(string subject = "CN=AvaWeather Development")
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest(subject, key,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
    }

    private static string Sha256(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
}
