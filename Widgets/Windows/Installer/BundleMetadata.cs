namespace AvaWeather.Widget.Installer;

internal static partial class BundleMetadata
{
    internal static (string Package, string Certificate) Hashes
    {
        get
        {
            string? package = null, certificate = null;
            Populate(ref package, ref certificate);
            return (package ?? string.Empty, certificate ?? string.Empty);
        }
    }

    static partial void Populate(ref string? package, ref string? certificate);
}
