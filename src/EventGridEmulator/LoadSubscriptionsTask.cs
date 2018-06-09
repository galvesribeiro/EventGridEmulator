using EventGridEmulator.Grains;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EventGridEmulator
{
    public class LoadSubscriptionsTask : IStartupTask
    {
        private readonly ILogger _logger;
        private readonly IGrainFactory _grainFactory;

        public LoadSubscriptionsTask(ILoggerFactory loggerFactory, IGrainFactory grainFactory)
        {
            this._logger = loggerFactory.CreateLogger<LoadSubscriptionsTask>();
            this._grainFactory = grainFactory;
        }

        public async Task Execute(CancellationToken cancellationToken)
        {
            this._logger.Info("Loading existent subscriptions...");

            var subscriptionManager = this._grainFactory.GetGrain<ISubscriptionManagerGrain>(Guid.Empty);
            await subscriptionManager.Initialize();

            this._logger.Info("Subscriptions loaded!");
        }
    }
}
