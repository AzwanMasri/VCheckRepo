using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Local_API.Services
{
    public class APIServer
    {
        private WebApplication? _app;

        public async Task StartAsync()
        {
            var builder = WebApplication.CreateBuilder();

            builder.WebHost.UseUrls("http://localhost:8080");

            builder.Services.AddControllers();

            _app = builder.Build();

            _app.MapControllers();

            await _app.StartAsync();
        }

        public async Task StopAsync()
        {
            if (_app != null)
            {
                await _app.StopAsync();
                await _app.DisposeAsync();
            }
        }
    }
}
