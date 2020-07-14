using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace poc_azure_keyvault
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using var host = CreateHost();
            await host.StartAsync();
            var provider = host.Services.GetService<Provider>();
            provider.Start();

            Console.ReadLine();
        }


        private static IHost CreateHost()
        {
            var builder = new HostBuilder();

            builder.ConfigureAppConfiguration((context, builder) =>
            {
                builder.Sources.Clear();
                builder
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false)
                    .AddEnvironmentVariables();
            });

            builder.ConfigureServices((context, builder) =>
            {
                builder.AddLogging(c => c.AddConsole()).Configure<LoggerFilterOptions>(cfg => cfg.MinLevel = LogLevel.Debug);
                builder.AddScoped<Provider>();
                builder.Configure<Settings>(context.Configuration.GetSection("Settings"));
            });


            return builder.Build();
        }
    }
}
