namespace Culex.WeatherForecast;

public interface IWeatherForecastGrain
{
    Task<List<WeatherForecast>> GetForecastAsync();
}
