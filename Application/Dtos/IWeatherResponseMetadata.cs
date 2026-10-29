namespace Application.Dtos;

/// <summary>IWeatherResponseMetadata. Keep backend retention metadata outside native provider JSON.</summary>
public interface IWeatherResponseMetadata
{
    DateTimeOffset? ServerCachedAtUtc { get; set; }
}
