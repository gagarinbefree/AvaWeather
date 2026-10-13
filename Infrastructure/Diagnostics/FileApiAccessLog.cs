#if DEBUG
using System.Text;
using Application.Interfaces;

namespace AvaWeather.Diagnostics;

public sealed class FileApiAccessLog : IApiAccessLog
{
    private readonly string _path;
    private readonly object _gate = new();

    public FileApiAccessLog(string path)
    {
        _path = path;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        Write(new ApiAccessEvent("AvaWeather", "startup", "DebugLoggingEnabled", 0));
    }

    public void Write(ApiAccessEvent entry)
    {
        var line = new StringBuilder()
            .Append(DateTimeOffset.Now.ToString("O"))
            .Append(' ').Append(entry.Service)
            .Append(' ').Append(entry.Operation)
            .Append(' ').Append(entry.Outcome)
            .Append(" duration_ms=").Append(entry.DurationMs);
        if (entry.StatusCode is int status) line.Append(" HTTP=").Append(status);
        if (entry.LocationMode is not null) line.Append(" location=").Append(entry.LocationMode);
        if (entry.ConnectionRoute is not null) line.Append(" route=").Append(entry.ConnectionRoute);
        if (entry.CredentialSource is not null) line.Append(" credential=").Append(entry.CredentialSource);
        if (entry.ErrorType is not null) line.Append(" error=").Append(entry.ErrorType);
        if (entry.NetworkError is not null) line.Append(" network=").Append(entry.NetworkError);
        line.AppendLine();

        lock (_gate) File.AppendAllText(_path, line.ToString(), Encoding.UTF8);
    }
}
#endif
