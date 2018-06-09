using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Orleans;
using System.Threading;
using System.Threading.Tasks;

namespace EventGridEmulator
{
    public class Program
    {
        public static CancellationTokenSource CancellationToken = new CancellationTokenSource();

        public static async Task Main(string[] args)
        {
            var clusterClient = await OrleansHost.Start(CancellationToken.Token);
            await CreateWebHostBuilder(args, clusterClient).Build().RunAsync();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args, IClusterClient clusterClient) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .ConfigureAppConfiguration(builder => builder.AddJsonFile("appsettings.json"))
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                    logging.AddDebug();
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton(clusterClient);
                });
    }
}
