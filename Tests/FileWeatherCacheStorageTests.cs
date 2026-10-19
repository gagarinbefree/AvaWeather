using Infrastructure.Caching;

namespace Tests;

public class FileWeatherCacheStorageTests
{
    [Fact]
    public async Task Missing_file_returns_null_and_writes_survive_new_instance()
    {
        var folder = Path.Combine(Path.GetTempPath(), "AvaWeather-cache-test-" + Guid.NewGuid());
        try
        {
            var path = Path.Combine(folder, "weather.json");
            var storage = new FileWeatherCacheStorage(path);
            var token = TestContext.Current.CancellationToken;
            Assert.Null(await storage.ReadAsync(token));
            await storage.WriteAsync("first", token);
            await storage.WriteAsync("second", token);
            Assert.Equal("second", await new FileWeatherCacheStorage(path).ReadAsync(token));
            Assert.Equal(["weather.json"], Directory.GetFiles(folder).Select(Path.GetFileName));
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
    }
}
