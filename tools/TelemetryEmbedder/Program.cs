using System.Text.Json;
using Localization;

if (args.Length != 1)
    throw new ArgumentException(StringLocalizer.Current.Get("TelemetryOutputRequired"));

var source = $$"""
    // Generated for a release build. The telemetry DSN is a public project identifier.
    #nullable enable
    namespace AvaWeather.Services;

    public static partial class EmbeddedWeatherTelemetryDsn
    {
        /// <summary>Populate. Supply the optional public telemetry destination.</summary>
        /// <param name="dsn">The public telemetry project identifier used by this release.</param>
        static partial void Populate(ref string? dsn)
        {
            dsn = {{JsonSerializer.Serialize(Environment.GetEnvironmentVariable("HAWK_DSN"))}};
        }
    }
    """;
var output = Path.GetFullPath(args[0]);
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
File.WriteAllText(output, source);
Console.WriteLine(StringLocalizer.Current.Get("TelemetrySourceGenerated"));
