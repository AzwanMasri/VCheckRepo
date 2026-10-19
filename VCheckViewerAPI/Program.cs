using log4net.Config;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;
using VCheck.Lib.Data.Models;
using VCheckViewerAPI.Lib.Util;
using VCheckViewerAPI.Message.General;
using VCheckViewerAPI.Services;


XmlConfigurator.Configure(log4net.LogManager.GetRepository(Assembly.GetEntryAssembly()),
                                  new FileInfo("log4Net.config"));

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("IpRateLimit", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString()
                 ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ip,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 1,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHostedService<BackgroundServices>();

var app = builder.Build();

app.UseRateLimiter();

//// Configure the HTTP request pipeline.
app.UseSwagger();
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwaggerUI();
//}

app.UseHttpsRedirection();


app.UseAuthorization();


app.UseExceptionHandler(a => a.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    string? ClientKey;
    object? requestJson = null;

    Logger sLogger = new Logger();
    sLogger.Error("Internal Error >>> " ,error);

    //context.Request.EnableBuffering();

    context.Request.Body.Seek(0, SeekOrigin.Begin);

    using (StreamReader stream = new StreamReader(context.Request.Body, Encoding.UTF8, true, 1024, true))
    {
        string body = await stream.ReadToEndAsync();
        requestJson = JsonConvert.DeserializeObject(body);
        ClientKey = JsonConvert.DeserializeObject<HeaderModel>(body)?.clientKey;
    }

    context.Request.Body.Position = 0;

    var responseHeader = new HeaderModel() { timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), clientKey = ClientKey };
    var responseBody = new ResponseBody() { ResponseCode = "VV.9999", ResponseStatus = "Fail", ResponseMessage = "Internal Error", Results = null };
    var response = new ResponseModel() { Header = responseHeader, Body = responseBody };

    var responseJson = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(response));

    //sLogger.ApiLog(requestJson, responseJson);
    //APILogging sAPILogging = new APILogging();
    //sAPILogging.ApiLog(context, responseJson);
}));


app.Use(async (context, next) =>
    {
        context.Request.EnableBuffering();

        await next();
    });

//app.UseMiddleware<ApiHandlerMiddleware>();

//app.UseEndpoints(endpoints =>
//{
//    endpoints.MapRazorPages();
//    endpoints.MapControllers();
//});

app.MapControllers();

app.Run();
