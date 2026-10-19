using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Application.Diagnostics;

namespace Infrastructure.Diagnostics;

/// <summary>Sends a bounded, sanitized weather failure as a Sentry-compatible event.</summary>
public sealed class SentryWeatherFailureReporter : IWeatherFailureReporter
{
    private static readonly TimeSpan RepeatInterval = TimeSpan.FromMinutes(10);
    private readonly HttpClient _client;
    private readonly WeatherDiagnosticLog _diagnostics;
    private readonly Uri? _endpoint;
    private readonly string? _publicKey;
    private readonly Dictionary<WeatherFailureKind, DateTimeOffset> _lastReport = [];
    private readonly object _gate = new();

    public SentryWeatherFailureReporter(HttpClient client, string? dsn, WeatherDiagnosticLog diagnostics)
    {
        _client = client;
        _diagnostics = diagnostics;
        if (TryParseDsn(dsn, out var endpoint, out var publicKey))
        {
            _endpoint = endpoint;
            _publicKey = publicKey;
        }
    }

    public void Report(WeatherFailureKind failure) => _ = ReportAsync(failure);

    public async Task ReportAsync(WeatherFailureKind failure)
    {
        if (_endpoint is null || _publicKey is null) return;

        var now = DateTimeOffset.UtcNow;
        lock (_gate)
        {
            if (_lastReport.TryGetValue(failure, out var last) && now - last < RepeatInterval) return;
            _lastReport[failure] = now;
        }

        try
        {
            var snapshot = _diagnostics.CreateSafeSnapshot();
            using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint);
            request.Headers.TryAddWithoutValidation("X-Sentry-Auth",
                $"Sentry sentry_version=7, sentry_client=avaweather/1.0, sentry_key={_publicKey}");
            request.Content = new StringContent(CreateEnvelope(failure, snapshot), Encoding.UTF8);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-sentry-envelope");
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(4));
            using var response = await _client.SendAsync(request, timeout.Token).ConfigureAwait(false);
            // A rejected telemetry request must not change the weather error or trigger a retry loop.
        }
        catch (Exception)
        {
            // The user's forecast and Retry button are independent of diagnostics transport.
        }
    }

    private static bool TryParseDsn(string? dsn, out Uri? endpoint, out string? publicKey)
    {
        endpoint = null;
        publicKey = null;
        if (string.IsNullOrWhiteSpace(dsn) || !Uri.TryCreate(dsn, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps || uri.Query.Length != 0 || uri.Fragment.Length != 0)
            return false;

        var key = uri.UserInfo.Split(':')[0];
        var segments = uri.AbsolutePath.Trim('/').Split('/');
        if (key.Length is < 1 or > 128 || !key.All(char.IsAsciiLetterOrDigit) ||
            segments.Length == 0 || !long.TryParse(segments[^1], NumberStyles.None,
                CultureInfo.InvariantCulture, out _)) return false;

        var prefix = string.Join('/', segments.SkipLast(1));
        var path = string.IsNullOrEmpty(prefix) ? "" : $"/{prefix}";
        endpoint = new UriBuilder(uri.Scheme, uri.Host, uri.Port,
            $"{path}/api/{segments[^1]}/envelope/").Uri;
        publicKey = key;
        return true;
    }

    private static string CreateEnvelope(WeatherFailureKind failure, WeatherFailureSnapshot snapshot)
    {
        var id = Guid.NewGuid().ToString("N");
        var body = $"Weather loading failed: {failure}";
        var header = Json(writer =>
        {
            writer.WriteString("event_id", id);
            writer.WriteString("sent_at", snapshot.OccurredAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
        });
        var @event = Json(writer =>
        {
            writer.WriteString("event_id", id);
            writer.WriteString("timestamp", snapshot.OccurredAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
            writer.WriteString("platform", "csharp");
            writer.WriteString("level", "error");
            writer.WriteString("message", body);
            writer.WriteString("release", snapshot.Version);
            writer.WriteStartObject("tags");
            writer.WriteString("platform", snapshot.Platform);
            writer.WriteString("failure", failure.ToString());
            writer.WriteEndObject();
            writer.WriteStartArray("fingerprint");
            writer.WriteStringValue("AvaWeather weather loading");
            writer.WriteStringValue(failure.ToString());
            writer.WriteEndArray();
            writer.WriteStartObject("extra");
            writer.WriteStartArray("recent_requests");
            foreach (var item in snapshot.RecentRequests) writer.WriteStringValue(item);
            writer.WriteEndArray();
            writer.WriteEndObject();
        });
        return $"{header}\n{{\"type\":\"event\",\"content_type\":\"application/json\"}}\n{@event}\n";
    }

    private static string Json(Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            write(writer);
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
