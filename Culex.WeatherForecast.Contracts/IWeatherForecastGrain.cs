namespace Culex.WeatherForecast;

using Orleans;

public interface IWeatherForecastGrain : IGrainWithGuidKey
{
    Task<List<WeatherForecast>> GetForecastAsync();
}
