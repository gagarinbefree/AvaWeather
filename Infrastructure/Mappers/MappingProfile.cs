using AutoMapper;
using Application.Dtos;
using Domain.Entities;

namespace Infrastructure.Mappers;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<LocationDto, WeatherData>()
            .ForMember(dest => dest.LocalTime, opt => opt.MapFrom(src =>
                string.IsNullOrEmpty(src.LocalTime) ? DateTime.Now : DateTime.Parse(src.LocalTime)));

        CreateMap<CurrentDataDto, CurrentWeather>()
            .ForMember(dest => dest.LastUpdated, opt => opt.MapFrom(src =>
                string.IsNullOrEmpty(src.LastUpdated) ? DateTime.Now : DateTime.Parse(src.LastUpdated)))
            .ForMember(dest => dest.ConditionCode, opt => opt.MapFrom(src => src.Condition.Code))
            .ForMember(dest => dest.ConditionText, opt => opt.MapFrom(src => src.Condition.Text))
            .ForMember(dest => dest.ConditionIcon, opt => opt.MapFrom(src => src.Condition.Icon));

        CreateMap<HourDto, HourlyForecast>()
            .ForMember(dest => dest.Time, opt => opt.MapFrom(src =>
                string.IsNullOrEmpty(src.Time) ? DateTime.Now : DateTime.Parse(src.Time)))
            .ForMember(dest => dest.ConditionCode, opt => opt.MapFrom(src => src.Condition.Code))
            .ForMember(dest => dest.ConditionText, opt => opt.MapFrom(src => src.Condition.Text))
            .ForMember(dest => dest.ConditionIcon, opt => opt.MapFrom(src => src.Condition.Icon));

        CreateMap<ForecastDayDto, DailyForecast>()
            .ForMember(dest => dest.Date, opt => opt.MapFrom(src =>
                string.IsNullOrEmpty(src.Date) ? DateTime.Now : DateTime.Parse(src.Date)))
            .ForMember(dest => dest.MaxtempC, opt => opt.MapFrom(src => src.Day.MaxtempC))
            .ForMember(dest => dest.MintempC, opt => opt.MapFrom(src => src.Day.MintempC))
            .ForMember(dest => dest.AvgtempC, opt => opt.MapFrom(src => src.Day.AvgtempC))
            .ForMember(dest => dest.ConditionCode, opt => opt.MapFrom(src => src.Day.Condition.Code))
            .ForMember(dest => dest.ConditionText, opt => opt.MapFrom(src => src.Day.Condition.Text))
            .ForMember(dest => dest.ConditionIcon, opt => opt.MapFrom(src => src.Day.Condition.Icon))
            .ForMember(dest => dest.MaxwindKph, opt => opt.MapFrom(src => src.Day.MaxwindKph))
            .ForMember(dest => dest.TotalprecipMm, opt => opt.MapFrom(src => src.Day.TotalprecipMm))
            .ForMember(dest => dest.Avghumidity, opt => opt.MapFrom(src => src.Day.Avghumidity))
            .ForMember(dest => dest.Uv, opt => opt.MapFrom(src => src.Day.Uv))
            .ForMember(dest => dest.DailyChanceOfRain, opt => opt.MapFrom(src => src.Day.DailyChanceOfRain))
            .ForMember(dest => dest.Sunrise, opt => opt.MapFrom(src => src.Astro.Sunrise))
            .ForMember(dest => dest.Sunset, opt => opt.MapFrom(src => src.Astro.Sunset));

        CreateMap<DayDto, DailyForecast>()
            .ForMember(dest => dest.ConditionCode, opt => opt.MapFrom(src => src.Condition.Code))
            .ForMember(dest => dest.ConditionText, opt => opt.MapFrom(src => src.Condition.Text))
            .ForMember(dest => dest.ConditionIcon, opt => opt.MapFrom(src => src.Condition.Icon));
    }
}