namespace Culex.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using Culex.WeatherForecast;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;
    private readonly IWeatherForecastGrain weatherForecastGrain;
    public WeatherForecastController(ILogger<WeatherForecastController> logger, IWeatherForecastGrain weatherForecastGrain)
    {
        _logger = logger;
        this.weatherForecastGrain = weatherForecastGrain;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public async Task<IEnumerable<WeatherForecast>> Get()
    {
        var result = await this.weatherForecastGrain.GetForecastAsync();

        return result;
    }
}
