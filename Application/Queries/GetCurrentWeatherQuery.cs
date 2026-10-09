using Application.Dtos;
using MediatR;

namespace Application.Queries;

public record GetCurrentWeatherQuery(string? Location = null) : IRequest<CurrentResponseDto>;
