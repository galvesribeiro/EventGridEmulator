using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EventGridEmulator
{
    public static class OrleansHost
    {
        public static async Task<IClusterClient> Start(CancellationToken cancellationToken)
        {
            var thisAssembly = typeof(OrleansHost).Assembly;
            var host = new SiloHostBuilder()
                .ConfigureHostConfiguration(builder => builder.AddJsonFile("appsettings.json"))
                .AddMemoryGrainStorageAsDefault()
                .AddMemoryGrainStorage("PubSubStore")
                .AddSimpleMessageStreamProvider(Constants.STREAM_PROVIDER, DefaultSMSSettings)
                .AddStartupTask<LoadSubscriptionsTask>()
                .UseLocalhostClustering()
                .Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback)
                .UseInMemoryReminderService()
                .ConfigureLogging((context, logging) =>
                {
                    logging.AddConfiguration(context.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                })
                .ConfigureApplicationParts(appPartManager =>
                {
                    appPartManager.AddApplicationPart(thisAssembly).WithCodeGeneration();
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton(new HttpClient());
                })
                .Build();

            await host.StartAsync(cancellationToken);

            var client = new ClientBuilder()
                .ConfigureHostConfiguration(builder => builder.AddJsonFile("appsettings.json"))
                .UseLocalhostClustering()
                .ConfigureApplicationParts(appPartManager =>
                {
                    appPartManager.AddApplicationPart(thisAssembly).WithCodeGeneration();
                })
                .Build();

            await client.Connect();
            return client;
        }

        private static void DefaultSMSSettings(SimpleMessageStreamProviderOptions options)
        {
            options.FireAndForgetDelivery = true;
        }
    }
}
