using Application.Diagnostics;
using Application.Interfaces;
using System.Globalization;

namespace Tests;

public class WeatherDiagnosticLogTests
{
    [Fact]
    public void Issue_report_contains_recent_safe_request_facts_only()
    {
        var log = new WeatherDiagnosticLog(platform: "Android", version: "1.0.91");
        log.Write(new ApiAccessEvent("WeatherAPI", "forecast", "TransportError", 22114,
            ErrorType: "SocketException", NetworkError: "ConnectionError",
            LocationMode: "coordinates", CredentialSource: "secret-API-KEY",
            ConnectionRoute: "system"));
        log.Write(new ApiAccessEvent("WeatherAPI", "current", "HttpError", 320,
            StatusCode: 503, ErrorType: "188.16.96.136", LocationMode: "58.0047,56.2514"));

        var uri = log.CreateIssueUri(WeatherFailureKind.Connection);
        var report = Uri.UnescapeDataString(uri.Query);

        Assert.Equal("https://github.com/gagarinbefree/AvaWeather/issues/new", uri.GetLeftPart(UriPartial.Path));
        Assert.Contains("Android", report);
        Assert.Contains("1.0.91", report);
        Assert.Contains("TransportError", report);
        Assert.Contains("22114", report);
        Assert.Contains("503", report);
        Assert.Contains("coordinates", report);
        Assert.DoesNotContain("secret-API-KEY", report);
        Assert.DoesNotContain("188.16.96.136", report);
        Assert.DoesNotContain("58.0047,56.2514", report);
    }

    [Fact]
    public void Issue_report_is_bounded_and_debug_sink_still_receives_events()
    {
        var sink = new RecordingLog();
        var log = new WeatherDiagnosticLog(sink, platform: "Browser/PWA", version: "1.0.91");
        for (var index = 0; index < 30; index++)
            log.Write(new ApiAccessEvent("WeatherAPI", "forecast", "Timeout", index));

        var report = Uri.UnescapeDataString(log.CreateIssueUri(WeatherFailureKind.Timeout).Query);
        Assert.Equal(30, sink.Events.Count);
        Assert.DoesNotContain(" 0ms", report);
        Assert.Contains(" 29ms", report);
        Assert.True(log.CreateIssueUri(WeatherFailureKind.Timeout).AbsoluteUri.Length < 3000);
    }

    [Fact]
    public void Issue_report_uses_the_interface_language()
    {
        var log = new WeatherDiagnosticLog(platform: "Android", version: "1.0.91");
        var report = Uri.UnescapeDataString(log.CreateIssueUri(
            WeatherFailureKind.Connection, CultureInfo.GetCultureInfo("ru-RU")).Query);

        Assert.Contains("Не удалось загрузить погоду", report);
        Assert.Contains("Версия AvaWeather: 1.0.91", report);
        Assert.Contains("Отчёт будет публичным", report);
    }

    private sealed class RecordingLog : IApiAccessLog
    {
        public List<ApiAccessEvent> Events { get; } = [];
        public void Write(ApiAccessEvent entry) => Events.Add(entry);
    }
}
