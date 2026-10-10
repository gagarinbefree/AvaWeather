#if DEBUG
using Application.Interfaces;
using AvaWeather.Diagnostics;

namespace AvaWeather.Tests;

public class DebugApiAccessLogTests
{
    [Fact]
    public void Debug_log_creates_log_file_with_safe_fields()
    {
        var directory = Path.Combine(Path.GetTempPath(), "AvaWeather-log-test-" + Guid.NewGuid());
        var path = Path.Combine(directory, "api-access.log");
        try
        {
            var log = new FileApiAccessLog(path);
            log.Write(new ApiAccessEvent("WeatherAPI", "current", "HttpError", 42,
                StatusCode: 401, LocationMode: "coordinates"));

            var content = File.ReadAllText(path);
            Assert.Contains("WeatherAPI current", content);
            Assert.Contains("HTTP=401", content);
            Assert.Contains("location=coordinates", content);
            Assert.DoesNotContain("key=", content);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
#else
namespace AvaWeather.Tests;

public class DebugApiAccessLogTests
{
    [Fact]
    public void Release_build_does_not_contain_file_log_writer() =>
        Assert.Null(typeof(App).Assembly.GetType("AvaWeather.Diagnostics.FileApiAccessLog"));
}
#endif
