namespace Application.Interfaces;

public sealed record ApiAccessEvent(
    string Service,
    string Operation,
    string Outcome,
    long DurationMs,
    int? StatusCode = null,
    string? ErrorType = null,
    string? NetworkError = null,
    string? LocationMode = null,
    string? CredentialSource = null,
    string? ConnectionRoute = null);

public interface IApiAccessLog
{
    void Write(ApiAccessEvent entry);
}
