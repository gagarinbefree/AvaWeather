using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

namespace AvaWeather.Widget.Installer;

internal static class InstallerService
{
    private const string CertificateResource = "AvaWeather.Widget.cer";
    private const string PackageResource = "AvaWeather.Widget.msix";

    public static string CertificateFingerprint()
    {
        using var certificate = X509CertificateLoader.LoadCertificate(ReadResource(CertificateResource));
        return certificate.GetCertHashString(System.Security.Cryptography.HashAlgorithmName.SHA256);
    }

    public static void TrustCertificateElevated()
    {
        var (package, certificateBytes) = ReadAndVerify();
        _ = package;
        using var certificate = X509CertificateLoader.LoadCertificate(certificateBytes);
        using var store = new X509Store(StoreName.TrustedPeople, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadWrite);
        if (store.Certificates.Find(X509FindType.FindByThumbprint, certificate.Thumbprint, false).Count == 0)
            store.Add(certificate);
    }

    public static async Task InstallAsync()
    {
        var (package, certificateBytes) = ReadAndVerify();
        using var certificate = X509CertificateLoader.LoadCertificate(certificateBytes);
        var temporary = Path.Combine(Path.GetTempPath(), "AvaWeatherWidget-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporary);
        try
        {
            var msixPath = Path.Combine(temporary, "AvaWeather.Widget.msix");
            await File.WriteAllBytesAsync(msixPath, package);
            await RunPowerShellAsync(temporary, "verify.ps1", """
                param([string]$PackagePath, [string]$Thumbprint)
                $ErrorActionPreference = 'Stop'
                $signature = Get-AuthenticodeSignature -LiteralPath $PackagePath
                if ($null -eq $signature.SignerCertificate -or
                    $signature.SignerCertificate.Thumbprint -ne $Thumbprint) {
                    throw 'MSIX signer does not match the bundled certificate.'
                }
                """, "-PackagePath", msixPath, "-Thumbprint", certificate.Thumbprint);

            if (!IsTrusted(certificate)) await TrustWithElevationAsync();
            await RunPowerShellAsync(temporary, "install.ps1", """
                param([string]$PackagePath)
                $ErrorActionPreference = 'Stop'
                Add-AppxPackage -Path $PackagePath -ErrorAction Stop
                """, "-PackagePath", msixPath);
        }
        finally
        {
            Directory.Delete(temporary, recursive: true);
        }
    }

    private static bool IsTrusted(X509Certificate2 certificate)
    {
        using var store = new X509Store(StoreName.TrustedPeople, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadOnly);
        return store.Certificates.Find(X509FindType.FindByThumbprint, certificate.Thumbprint, false).Count > 0;
    }

    private static async Task TrustWithElevationAsync()
    {
        var start = new ProcessStartInfo(Environment.ProcessPath!, "--trust-cert")
        {
            UseShellExecute = true,
            Verb = "runas"
        };
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start the certificate helper.");
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
            throw new InvalidOperationException("The signing certificate was not trusted by Windows.");
    }

    private static async Task RunPowerShellAsync(string directory, string scriptName,
        string script, params string[] arguments)
    {
        var scriptPath = Path.Combine(directory, scriptName);
        await File.WriteAllTextAsync(scriptPath, script);
        var executable = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            "System32", "WindowsPowerShell", "v1.0", "powershell.exe");
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        foreach (var argument in new[] { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", scriptPath }.Concat(arguments))
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start PowerShell.");
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
            throw new InvalidOperationException((await error).Trim() is { Length: > 0 } message
                ? message : (await output).Trim());
        await Task.WhenAll(error, output);
    }

    private static (byte[] Package, byte[] Certificate) ReadAndVerify()
    {
        var package = ReadResource(PackageResource);
        var certificate = ReadResource(CertificateResource);
        var hashes = BundleMetadata.Hashes;
        using var signer = InstallerPayload.Verify(package, certificate, hashes.Package, hashes.Certificate);
        return (package, certificate);
    }

    private static byte[] ReadResource(string resource)
    {
        using var stream = typeof(InstallerService).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidDataException($"Missing embedded resource: {resource}");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
