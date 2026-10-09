using Application.Dtos;
using MediatR;

namespace Application.Queries;

public record GetForecastQuery : IRequest<ForecastResponseDto>;