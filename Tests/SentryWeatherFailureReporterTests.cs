using System.Net;
using System.Text.Json;
using Application.Diagnostics;
using Application.Interfaces;
using Infrastructure.Diagnostics;

namespace Tests;

public class SentryWeatherFailureReporterTests
{
    [Fact]
    public async Task Sends_only_sanitized_failure_to_sentry_envelope_endpoint()
    {
        var handler = new RecordingHandler();
        using var client = new HttpClient(handler);
        var log = new WeatherDiagnosticLog(platform: "Android", version: "1.0.91");
        log.Write(new ApiAccessEvent("WeatherAPI", "forecast", "TransportError", 120,
            ErrorType: "secret-key", LocationMode: "58.0,56.2", CredentialSource: "secret-key"));
        var reporter = new SentryWeatherFailureReporter(client,
            "https://public@example.org/sentry/42", log);

        await reporter.ReportAsync(WeatherFailureKind.Connection);

        Assert.Equal(new Uri("https://example.org/sentry/api/42/envelope/"), handler.RequestUri);
        Assert.Contains("sentry_key=public", handler.Authorization);
        Assert.Contains("\"type\":\"event\"", handler.Body);
        Assert.DoesNotContain("\"type\":\"log\"", handler.Body);
        Assert.Contains("Weather loading failed: Connection", handler.Body);
        Assert.Contains("TransportError", handler.Body);
        Assert.Contains("Android", handler.Body);
        Assert.DoesNotContain("secret-key", handler.Body);
        Assert.DoesNotContain("58.0,56.2", handler.Body);
        using var payload = JsonDocument.Parse(handler.Body.Split('\n')[2]);
        Assert.Equal("1.0.91", payload.RootElement.GetProperty("release").GetString());
    }

    [Fact]
    public async Task Missing_dsn_disables_upload_and_duplicate_failures_are_limited()
    {
        var handler = new RecordingHandler();
        using var client = new HttpClient(handler);
        var log = new WeatherDiagnosticLog();
        await new SentryWeatherFailureReporter(client, null, log)
            .ReportAsync(WeatherFailureKind.Timeout);
        Assert.Equal(0, handler.Count);

        var reporter = new SentryWeatherFailureReporter(client, "https://public@example.org/42", log);
        await reporter.ReportAsync(WeatherFailureKind.Timeout);
        await reporter.ReportAsync(WeatherFailureKind.Timeout);
        await reporter.ReportAsync(WeatherFailureKind.Connection);
        Assert.Equal(2, handler.Count);
    }

    [Fact]
    public async Task Telemetry_transport_failure_does_not_throw()
    {
        using var client = new HttpClient(new RecordingHandler(throwOnSend: true));
        var reporter = new SentryWeatherFailureReporter(client,
            "https://public@example.org/42", new WeatherDiagnosticLog());

        await reporter.ReportAsync(WeatherFailureKind.Unexpected);
    }

    private sealed class RecordingHandler(bool throwOnSend = false) : HttpMessageHandler
    {
        public int Count { get; private set; }
        public Uri? RequestUri { get; private set; }
        public string? Authorization { get; private set; }
        public string Body { get; private set; } = "";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Count++;
            if (throwOnSend) throw new HttpRequestException("offline");
            RequestUri = request.RequestUri;
            Authorization = request.Headers.GetValues("X-Sentry-Auth").Single();
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
