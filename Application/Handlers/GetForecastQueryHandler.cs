using Application.Dtos;
using Application.Interfaces;
using MediatR;

namespace Application.Queries;

public class GetForecastQueryHandler : IRequestHandler<GetForecastQuery, ForecastResponseDto>
{
    private readonly IWeatherApiClient _weatherApiClient;

    public GetForecastQueryHandler(IWeatherApiClient weatherApiClient)
    {
        _weatherApiClient = weatherApiClient;
    }

    public async Task<ForecastResponseDto> Handle(GetForecastQuery request, CancellationToken cancellationToken)
    {
        return await _weatherApiClient.GetForecastAsync();
    }
}