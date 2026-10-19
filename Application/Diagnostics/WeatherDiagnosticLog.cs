using System.Globalization;
using System.Text;
using Application.Interfaces;
using Localization;

namespace Application.Diagnostics;

public enum WeatherFailureKind { Connection, Timeout, Http, Unexpected }

public sealed record WeatherFailureSnapshot(string Platform, string Version,
    DateTimeOffset OccurredAt, string[] RecentRequests);

/// <summary>Keeps a short, sanitized request history for a user-reviewed GitHub issue.</summary>
public sealed class WeatherDiagnosticLog(
    IApiAccessLog? debugSink = null, string? platform = null, string? version = null) : IApiAccessLog
{
    private const int MaxEvents = 6;
    private const string IssueUrl = "https://github.com/gagarinbefree/AvaWeather/issues/new";
    private readonly Queue<string> _events = new();
    private readonly object _gate = new();

    public void Write(ApiAccessEvent entry)
    {
        var summary = Summarize(entry);
        lock (_gate)
        {
            _events.Enqueue(summary);
            while (_events.Count > MaxEvents) _events.Dequeue();
        }
        debugSink?.Write(entry);
    }

    public Uri CreateIssueUri(WeatherFailureKind failure, CultureInfo? culture = null)
    {
        var snapshot = CreateSafeSnapshot();
        var strings = StringLocalizer.For(culture ?? CultureInfo.GetCultureInfo("en-US"));
        var body = new StringBuilder()
            .AppendLine(strings.Get("IssueReportIntro"))
            .AppendLine()
            .Append(strings.Get("IssueReportVersion")).Append(": ").AppendLine(snapshot.Version)
            .Append(strings.Get("IssueReportPlatform")).Append(": ").AppendLine(snapshot.Platform)
            .Append(strings.Get("IssueReportFailure")).Append(": ").AppendLine(failure.ToString())
            .Append(strings.Get("IssueReportTime")).Append(": ").AppendLine(snapshot.OccurredAt.ToString("O", CultureInfo.InvariantCulture))
            .AppendLine()
            .Append(strings.Get("IssueReportRecent")).AppendLine(":");
        if (snapshot.RecentRequests.Length == 0) body.AppendLine(strings.Get("IssueReportNoEvents"));
        foreach (var item in snapshot.RecentRequests) body.Append("- ").AppendLine(item);
        body.AppendLine().AppendLine(strings.Get("IssueReportDetails"));

        return new Uri($"{IssueUrl}?title={Uri.EscapeDataString(strings.Get("IssueReportTitle"))}" +
            $"&body={Uri.EscapeDataString(body.ToString())}");
    }

    public WeatherFailureSnapshot CreateSafeSnapshot()
    {
        string[] events;
        lock (_gate) events = _events.ToArray();
        var environment = platform ?? (OperatingSystem.IsBrowser() ? "Browser/PWA" :
            OperatingSystem.IsAndroid() ? "Android" : "Desktop");
        var appVersion = version ?? typeof(WeatherDiagnosticLog).Assembly.GetName().Version?.ToString(3) ?? "unknown";
        return new WeatherFailureSnapshot(
            Known(environment, "Browser/PWA", "Android", "Desktop"),
            IsSafeVersion(appVersion) ? appVersion : "unknown", DateTimeOffset.UtcNow, events);
    }

    private static bool IsSafeVersion(string value) => value.Length is > 0 and <= 32 &&
        value.All(character => character is >= '0' and <= '9' or '.');

    private static string Summarize(ApiAccessEvent entry)
    {
        var result = new StringBuilder()
            .Append(DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture))
            .Append(' ').Append(Known(entry.Service,
                "WeatherAPI", "GeoJS", "OpenMeteo-Geocoding", "AvaWeather", "AndroidLocation", "BrowserLocation"))
            .Append(' ').Append(Known(entry.Operation,
                "current", "forecast", "location", "city-name", "startup", "configuration"))
            .Append(' ').Append(Known(entry.Outcome,
                "Start", "Success", "HttpError", "TransportError", "Timeout", "Canceled",
                "InvalidJson", "InvalidResponse", "PermissionDenied", "Unavailable",
                "DebugLoggingEnabled", "KeyAvailable", "MissingKey"))
            .Append(' ').Append(Math.Max(0, entry.DurationMs)).Append("ms");
        if (entry.StatusCode is >= 100 and <= 599) result.Append(" HTTP=").Append(entry.StatusCode);
        AddKnown(result, " location=", entry.LocationMode, "coordinates", "auto:ip", "configured");
        AddKnown(result, " route=", entry.ConnectionRoute, "system", "direct");
        AddKnown(result, " error=", entry.ErrorType,
            "SocketException", "HttpRequestException", "TaskCanceledException", "OperationCanceledException", "JSException");
        AddKnown(result, " network=", entry.NetworkError,
            "ConnectionError", "NameResolutionError", "SecureConnectionError", "HttpProtocolError",
            "InvalidResponse", "ResponseEnded", "Unknown");
        return result.ToString();
    }

    private static string Known(string? value, params string[] choices) =>
        value is not null && choices.Contains(value, StringComparer.Ordinal) ? value : "other";

    private static void AddKnown(StringBuilder target, string prefix, string? value, params string[] choices)
    {
        if (value is not null && choices.Contains(value, StringComparer.Ordinal))
            target.Append(prefix).Append(value);
    }
}
