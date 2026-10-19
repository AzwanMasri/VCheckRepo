// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ScheduleAutomation_Others;

var builder = Host.CreateDefaultBuilder(args);
builder.ConfigureAppConfiguration((hostContext, config) =>
{
    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
});

builder.ConfigureServices(services =>
{
    services.AddHostedService<Worker>();
})
.UseWindowsService();

var host = builder.Build();
host.Run();
