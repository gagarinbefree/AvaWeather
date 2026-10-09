using Application.Dtos;
using MediatR;

namespace Application.Queries;

public record GetForecastQuery(string Language) : IRequest<ForecastResponseDto>;
