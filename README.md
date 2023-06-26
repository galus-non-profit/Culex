# Culex

## Table of contents
1. [Project introduction](#project-introduction)
2. [Microsoft Orleans introduction](#microsoft-orleans-introduction)
3. [Project preparation](#project-preparation)
4. [Grains](#grains)
5. [Using Orleans](#using-orleans)
5. [Server](#server)
6. [Orleans Dashboard](#orleans-dashboard)
7. [Orleans with Docker or Kubernetes](#orleans-with-docker-or-kubernetes)
8. [Useful links](#useful-links)

## Project introduction
Implementation of [Orleans SDK](https://learn.microsoft.com/en-us/dotnet/orleans/) for .NET. 

## Microsoft Orleans introduction

**Orleans** is a .NET Core, cross-platform framework for building robust, scalable distributed applications. It is based on an **actor model**, where each actor in the model is a lightweight, concurrent, immutable object enveloping a state and its behavior. These actors communicate with each other via the use of asynchronous messages. Actors are simply logical objects called **grains** (more on this later) that exist virtually, thus making them always addressable, even on failures of executing servers. 

![image](https://raw.githubusercontent.com/dotnet/orleans/gh-pages/assets/logo_full.png)

## Project preparation

*Make sure to use **.NET 7.0** for this project*

1. Create new project folder

```bash
mkdir Culex/src
cd Culex
```

2. In main folder create basic WebApi and solutions:

```bash
dotnet new sln --name Culex
dotnet new webapi --name Culex.WebApi
dotnet sln add Culex.WebApi

```

3. Open folder in VSC:

```bash
code .
```

4. Remove `appsettings.development.json`.

## Grains

1. Create two new classes, where
- `Culex.WeatherForecast.Contracts` - includes grain contracts
- `Culex.WeatherForecast` - includes logic and usage of the grain interfaces

```sh
dotnet new classlib --name Culex.WeatherForecast.Contracts
dotnet new classlib --name Culex.WeatherForecast
```

- add both classes to solution

```sh
dotnet sln add Culex.WeatherForecast.Contracts
dotnet sln add Culex.WeatherForecast
```

2. Add references

- `Culex.WeatherForecast` has reference to `Culex.WeatherForecast.Contracts`

```sh
dotnet add Culex.WeatherForecast reference Culex.WeatherForecast.Contracts
```

- `Culex.WebApi` has reference to `Culex.WeatherForecast.Contracts` and `Culex.WeatherForecast`

```csharp
dotnet add Culex.WebApi reference Culex.WeatherForecast.Contracts
dotnet add Culex.WebApi reference Culex.WeatherForecast
```

3. Prepare grain interfaces

- add `RootNamespace` to `Culex.WeatherForecast.Contracts.csproj`

```csharp
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net7.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>Culex.WeatherForecast</RootNamespace> // added
  </PropertyGroup>

</Project>
```

- move `WeatherForecast.cs` class to `Culex.WeatherForecast.Contracts`

```csharp
namespace Culex.WeatherForecast;

public class WeatherForecast
{
    public DateTime Date { get; set; }
    public int TemperatureC { get; set; }
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    public string? Summary { get; set; }
}
```

- create a grain interface `IWeatherForecastGrain` in `Culex.WeatherForecast.Contracts`

```csharp
namespace Culex.WeatherForecast;

public interface IWeatherForecastGrain
{
    Task<List<WeatherForecast>> GetForecastAsync();
}
```

4. Prepare the main grain

- create a class `WeatherForecastGrain` in `Culex.WeatherForecast` and move the logic from WebApi controller here.

```csharp
namespace Culex.WeatherForecast;

public class WeatherForecastGrain : IWeatherForecastGrain
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    public async Task<List<WeatherForecast>> GetForecastAsync()
    {
        var result = Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateTime.Now.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToList();

        return await Task.FromResult(result);
    }
}
```

5. Connect WeatherForecast controller to the grain

- add in `Culex.WebApi/Program.cs`

```csharp
builder.Services.AddScoped<IWeatherForecastGrain, WeatherForecastGrain>();
```

- use the grain in the controller `WeatherForecastController`

```csharp
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

    private readonly ILogger<WeatherForecastController> logger;
    private readonly IWeatherForecastGrain weatherForecastGrain;
    public WeatherForecastController(ILogger<WeatherForecastController> logger, IWeatherForecastGrain weatherForecastGrain)
    {
        this.logger = logger;
        this.weatherForecastGrain = weatherForecastGrain;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public async Task<IEnumerable<WeatherForecast>> Get()
    {
        var result = await this.weatherForecastGrain.GetForecastAsync();

        return result;
    }
}
```

If we run the application now using `dotnet run --project Culex.WebApi` command and go to `http://localhost:5048/swagger/index.html`, we should be able to run the request.


## Using Orleans

Since now we prepared grains, but we haven't used Microsoft Orleans yet. In this section we will improve our client (WebApi) and connect to grains.

1. In `Culex.WeatherForecast.Contracts`

- install Microsoft Orleans Sdk package

```sh
dotnet add Culex.WeatherForecast.Contracts package Microsoft.Orleans.Sdk
```

- use Orleans in the grain interface

*IWeatherForecastGrain.cs*

```csharp
namespace Culex.WeatherForecast;

using Orleans;

public interface IWeatherForecastGrain : IGrainWithGuidKey
{
    Task<List<WeatherForecast>> GetForecastAsync();
}
```

`IGrainWithGuidKey` - Marker interface for grains with Guid keys.

- use Orleans in the `WeatherForecast` class

*WeatherForecast.cs*

```csharp
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
```

2. In `Culex.WeatherForecast`

- improve `WeatherForecastGrain` class

```csharp
namespace Culex.WeatherForecast;

using Orleans;

public sealed class WeatherForecastGrain : Grain, IWeatherForecastGrain
{
    ...
}

```

`WeatherForecastGrain` should inherit from `Grain` class and implement `IWeatherForecastGrain`.

3. In `Culex.WebApi`

- add *Microsoft.Orleans.Client*

```sh
dotnet add Culex.WebApi package Microsoft.Orleans.Client
```

- improve Controller

```csharp
public class WeatherForecastController : ControllerBase
{
    private readonly ILogger<WeatherForecastController> this.logger;
    private readonly IGrainFactory grainFactory;
    public WeatherForecastController(ILogger<WeatherForecastController> logger, IGrainFactory grainFactory)
    {
        this.logger = logger;
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
```

- add Orleans Client configuration in `Program.cs`

```csharp
builder.Services.AddOrleansClient(clientBuilder => clientBuilder.UseLocalhostClustering());

// instead of builder.Services.AddScoped<IWeatherForecastGrain, WeatherForecastGrain>();
```

## Server
In this step, we will initialize a server that will host and run our grains.

1. Create a worker in main folder

```sh
dotnet new worker --name Culex.Worker
```

- remove *appsettings.development.json* file

- add reference in the Worker to `Culex.WeatherForecast.Contracts` and `Culex.WeatherForecast`

```sh
dotnet add Culex.Worker reference Culex.WeatherForecast.Contracts
dotnet add Culex.Worker reference Culex.WeatherForecast
```

- add `Culex.Worker` to solution

```sh
dotnet sln add Culex.Worker
```

2. Install Orleans Server in the worker

```sh
dotnet add Culex.Worker package Microsoft.Orleans.Server
```

3. Configure Silo in `Culex.Worker` `Package.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();
    siloBuilder.AddMemoryGrainStorage("weather");
});

var app = builder.Build();

app.Run();
```

- `UseOrleans` - configures the Silo,
- `UseLocalhostClustering` - configures the silo to use localhost clustering,
- `AddMemoryGrainStorage` - configures the Orleans silos to persist grains in memory.

4. Running the application

As you created the Server, open two tabs in the terminal:
- run our Server 

```sh
dotnet run --project Culex.Worker
```

- run our WebApi (Client)

```sh
dotnet run --project Culex.WebApi
```

It should be available to run the request through swagger. If we turn off the Server, we shouldn't be able to run WebApi.

## Orleans Dashboard
One popular Orleans tool is the OrleansDashboard NuGet package. This dashboard provides some simple metrics and insights into what is happening inside your Orleans app.

1. Add OrleansDashboard package to Worker

```sh
dotnet add Culex.Worker package OrleansDashboard
```

2. Configure Orleans Dashboard in `Program.cs`

```csharp
builder.Host.UseOrleans(siloBuilder =>
    ...
    siloBuilder.UseDashboard(options =>
        {
            options.Port = 8080;
            options.HostSelf = true;
            options.CounterUpdateIntervalMs = 5000;
        });
)
```

3. After running both applications, open the dashboard through `http://localhost:8080`

![image](https://github.com/OrleansContrib/OrleansDashboard/raw/master/screenshots/dashboard.png)

## Orleans with Docker or Kubernetes

It is possible to deploy Orleans applications by using [Docker](https://learn.microsoft.com/en-us/dotnet/orleans/deployment/docker-deployment) or [Kubernetes](https://learn.microsoft.com/en-us/dotnet/orleans/deployment/kubernetes).

## Useful links
- [Microsoft Orleans documentation](https://learn.microsoft.com/en-us/dotnet/orleans/)
- [Orleans Dashboard](https://github.com/OrleansContrib/OrleansDashboard)
- [Deploy Orleans app with Docker](https://learn.microsoft.com/en-us/dotnet/orleans/deployment/docker-deployment)
- [Deploy Orleans app with Kubernetes](https://learn.microsoft.com/en-us/dotnet/orleans/deployment/kubernetes)
