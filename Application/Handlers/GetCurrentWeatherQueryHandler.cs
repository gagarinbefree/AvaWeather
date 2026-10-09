using Application.Dtos;
using Application.Interfaces;
using MediatR;

namespace Application.Queries;

public class GetCurrentWeatherQueryHandler : IRequestHandler<GetCurrentWeatherQuery, CurrentResponseDto>
{
    private readonly IWeatherApiClient _weatherApiClient;

    public GetCurrentWeatherQueryHandler(IWeatherApiClient weatherApiClient)
    {
        _weatherApiClient = weatherApiClient;
    }

    public async Task<CurrentResponseDto> Handle(GetCurrentWeatherQuery request, CancellationToken cancellationToken)
    {
        return await _weatherApiClient.GetCurrentWeatherAsync();
    }
}