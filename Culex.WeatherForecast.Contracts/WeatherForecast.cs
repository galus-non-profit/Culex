namespace Culex.WeatherForecast;

using Orleans;

[GenerateSerializer]
public class WeatherForecast
{
    [Id(0)] public DateTime Date { get; set; }

    [Id(1)] public int TemperatureC { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    [Id(2)] public string? Summary { get; set; }
}
