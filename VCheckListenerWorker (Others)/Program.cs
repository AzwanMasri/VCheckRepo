using NHapi.Model.V23.Group;
using NHapi.Model.V231.Datatype;
using NHapi.Model.V24.Datatype;
using System.Reflection;
using VCheckListenerWorker_Others;
using VCheckListenerWorker_Others.Lib.DBContext;

//-------- Version 1 ----------//
//var builder = Host.CreateApplicationBuilder(args);
//builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
//builder.Services.AddHostedService<Worker>();

//var host = builder.Build();
//host.Run();


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


