using EventGridEmulator.Messages;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;
using Orleans.Streams;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace EventGridEmulator.Grains
{
    [ImplicitStreamSubscription(Constants.WebHookDispatcherNamespace)]
    public class WebHookDispatcherGrain : Grain, IEventDispatcherGrain, IAsyncObserver<Immutable<(string, SubscriptionProperties, EventGridEvent)>>
    {
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        public WebHookDispatcherGrain(ILoggerFactory loggerFactory, HttpClient httpClient)
        {
            this._logger = loggerFactory.CreateLogger<WebHookDispatcherGrain>();
            this._httpClient = httpClient;
        }

        public override async Task OnActivateAsync()
        {
            var streamProvider = this.GetStreamProvider(Constants.STREAM_PROVIDER);
            var stream = streamProvider.GetStream<Immutable<(string, SubscriptionProperties, EventGridEvent)>>(this.GetPrimaryKey(), Constants.WebHookDispatcherNamespace);
            var subscriptions = await stream.GetAllSubscriptionHandles();
            foreach (var subscription in subscriptions)
            {
                await subscription.ResumeAsync(this);
            }
        }

        public async Task Dispatch((string, SubscriptionProperties, EventGridEvent) eventGridEvent)
        {
            this._logger.Info($"Dispatching message: {JsonConvert.SerializeObject(eventGridEvent)}");

            var endpoint = eventGridEvent.Item2.Destination.Properties["endpointUrl"];
            var response = await this._httpClient.PostAsJsonAsync(endpoint, new[] { eventGridEvent.Item3 });

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                this._logger.LogWarning($"Webhook replied with '{response.StatusCode}' status. Expected 'OK' (200). We will try again in 5s.");
                RegisterTimer(async _=> {

                    await this.Dispatch(eventGridEvent);
                }, eventGridEvent, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(0));
            }
        }

        public Task OnNextAsync(Immutable<(string, SubscriptionProperties, EventGridEvent)> @event, StreamSequenceToken token = null) => this.Dispatch(@event.Value);

        public Task OnCompletedAsync() => Task.CompletedTask;

        public Task OnErrorAsync(Exception ex)
        {
            this._logger.LogError(ex, "Failure processing WebHook!");
            return Task.CompletedTask;
        }
    }
}
