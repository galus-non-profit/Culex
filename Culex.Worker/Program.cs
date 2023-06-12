var builder = WebApplication.CreateBuilder(args);

builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();
    siloBuilder.AddMemoryGrainStorage("weather");

    siloBuilder.UseDashboard(options =>
    {
        options.Port = 8080;
        options.HostSelf = true;
        options.CounterUpdateIntervalMs = 5000;
    });
});

var app = builder.Build();

app.Run();
