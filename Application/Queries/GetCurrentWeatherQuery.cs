using Application.Dtos;
using MediatR;

namespace Application.Queries;

public record GetCurrentWeatherQuery : IRequest<CurrentResponseDto>;