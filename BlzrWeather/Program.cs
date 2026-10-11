using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlzrWeather;
using BlzrWeather.Theming;
using Application.Extensions;
using Infrastructure.Configuration;
using Infrastructure.Extensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var options = new WeatherApiOptions
{
    BaseUrl = builder.Configuration["WeatherApi:BaseUrl"] ?? "https://api.weatherapi.com/v1/",
    ApiKey = builder.Configuration["WeatherApi:ApiKey"] ?? string.Empty,
    DefaultLocation = "auto:ip",
    ForecastDays = 3
};
builder.Services.AddLogging();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(options);
builder.Services.AddSingleton(ThemePalette.Load());

await builder.Build().RunAsync();
