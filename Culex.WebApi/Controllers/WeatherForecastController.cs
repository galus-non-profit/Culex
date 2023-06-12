namespace Culex.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using Culex.WeatherForecast;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly ILogger<WeatherForecastController> _logger;
    private readonly IGrainFactory grainFactory;
    public WeatherForecastController(ILogger<WeatherForecastController> logger, IGrainFactory grainFactory)
    {
        _logger = logger;
        this.grainFactory = grainFactory;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public async Task<IEnumerable<WeatherForecast>> Get()
    {
        var grain = this.grainFactory.GetGrain<IWeatherForecastGrain>(Guid.Empty);
        var result = await grain.GetForecastAsync();

        return result;
    }
}
