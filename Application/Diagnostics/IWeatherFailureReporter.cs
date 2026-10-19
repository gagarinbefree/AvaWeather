namespace Application.Diagnostics;

public interface IWeatherFailureReporter
{
    void Report(WeatherFailureKind failure);
}

public sealed class DisabledWeatherFailureReporter : IWeatherFailureReporter
{
    public static DisabledWeatherFailureReporter Instance { get; } = new();
    private DisabledWeatherFailureReporter() { }
    public void Report(WeatherFailureKind failure) { }
}
