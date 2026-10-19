using Application.Diagnostics;
using Application.Interfaces;

namespace Tests;

public class WeatherDiagnosticLogTests
{
    [Fact]
    public void Snapshot_contains_recent_safe_request_facts_only()
    {
        var log = new WeatherDiagnosticLog(platform: "Android", version: "1.0.91");
        log.Write(new ApiAccessEvent("WeatherAPI", "forecast", "TransportError", 22114,
            ErrorType: "SocketException", NetworkError: "ConnectionError",
            LocationMode: "coordinates", CredentialSource: "secret-API-KEY",
            ConnectionRoute: "system"));
        log.Write(new ApiAccessEvent("WeatherAPI", "current", "HttpError", 320,
            StatusCode: 503, ErrorType: "188.16.96.136", LocationMode: "58.0047,56.2514"));

        var snapshot = log.CreateSafeSnapshot();
        var report = string.Join(" ", snapshot.RecentRequests);

        Assert.Equal("Android", snapshot.Platform);
        Assert.Equal("1.0.91", snapshot.Version);
        Assert.Contains("TransportError", report);
        Assert.Contains("22114", report);
        Assert.Contains("503", report);
        Assert.Contains("coordinates", report);
        Assert.DoesNotContain("secret-API-KEY", report);
        Assert.DoesNotContain("188.16.96.136", report);
        Assert.DoesNotContain("58.0047,56.2514", report);
    }

    [Fact]
    public void Snapshot_is_bounded_and_debug_sink_still_receives_events()
    {
        var sink = new RecordingLog();
        var log = new WeatherDiagnosticLog(sink, platform: "Browser/PWA", version: "1.0.91");
        for (var index = 0; index < 30; index++)
            log.Write(new ApiAccessEvent("WeatherAPI", "forecast", "Timeout", index));

        var snapshot = log.CreateSafeSnapshot();
        var report = string.Join(" ", snapshot.RecentRequests);
        Assert.Equal(30, sink.Events.Count);
        Assert.DoesNotContain(" 0ms", report);
        Assert.Contains(" 29ms", report);
        Assert.Equal(6, snapshot.RecentRequests.Length);
    }

    private sealed class RecordingLog : IApiAccessLog
    {
        public List<ApiAccessEvent> Events { get; } = [];
        public void Write(ApiAccessEvent entry) => Events.Add(entry);
    }
}
