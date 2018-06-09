using EventGridEmulator.Messages;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EventGridEmulator.Grains
{
    public class SubscriptionManagerGrain : Grain, ISubscriptionManagerGrain
    {
        private const string ALL_EVENT_TYPES = "ALL";
        private const string SUBSCRIPTIONS_FILE_NAME = "subscriptions.json";
        private const string VALIDATE_ENDPOINT_EVENT_TYPE_NAME = "Microsoft.EventGrid.SubscriptionValidationEvent";
        private const string SIMULATOR_TOPIC = "EventGridEmulator";
        private static readonly string _dataDir = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "data");
        private static readonly string _dataFile = Path.Combine(_dataDir, SUBSCRIPTIONS_FILE_NAME);
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly Dictionary<string, SubscriptionProperties> _subscriptions = new Dictionary<string, SubscriptionProperties>();
        private IStreamProvider _streamProvider;

        public SubscriptionManagerGrain(ILoggerFactory loggerFactory, HttpClient client)
        {
            this._logger = loggerFactory.CreateLogger<SubscriptionManagerGrain>();
            this._httpClient = client;
        }

        public override Task OnActivateAsync()
        {
            this._streamProvider = this.GetStreamProvider(Constants.STREAM_PROVIDER);
            return base.OnActivateAsync();
        }

        public async Task CreateSubscription(string name, CreateSubscriptionRequest request)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            if (request == null) throw new ArgumentNullException(nameof(request));
            request.Validate();

            this._logger.Info($"Creating subscription '{name}'...");

            if (this._subscriptions.ContainsKey(name))
            {
                throw new InvalidOperationException($"A subscription already exist with the name '{name}'");
            }

            if (request.Properties.Destination.EndpointType == EndpointType.Webhook)
            {
                var endpointUrl = request.Properties.Destination.Properties["endpointUrl"];
                this._logger.Info($"Validating subscription '{name}' on endpoint {endpointUrl}");

                await ValidateWebHookEndpoint(name, endpointUrl);

                this._logger.Info($"Subscription '{name}' on endpoint {endpointUrl} was validated and created!");

                this._subscriptions[name] = request.Properties;
                await File.WriteAllTextAsync(_dataFile, JsonConvert.SerializeObject(this._subscriptions), Encoding.UTF8);
            }
            else
            {
                throw new NotSupportedException($"Unsupported endpoint type: {request.Properties.Destination.EndpointType}");
            }
        }

        private async Task ValidateWebHookEndpoint(string name, string endpointUrl)
        {
            var code = Guid.NewGuid();
            var data = new EndpointValidationData
            {
                ValidationCode = code,
                ValidationUrl = "http://localhost"
            };

            var validationRequest = new EventGridEvent
            {
                EventType = VALIDATE_ENDPOINT_EVENT_TYPE_NAME,
                EventTime = DateTime.UtcNow,
                Id = Guid.NewGuid(),
                Topic = SIMULATOR_TOPIC,
                Data = data
            };

            var response = await this._httpClient.PostAsJsonAsync(endpointUrl, new[] { validationRequest });
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Unable to validate endpoint '{endpointUrl}' for subscription '{name}'. ErrorCode: {response.StatusCode} | Error: {errorContent}");
            }

            var validationResponse = await response.Content.ReadAsAsync<EndpointValidationResponse>();
            if (!string.Equals(validationResponse.ValidationResponse.ToString(), code.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Unable to validate endpoint '{endpointUrl}' for subscription '{name}'. Expected code '{code.ToString()}' but received '{validationResponse.ValidationResponse}'.");
            }
        }

        public async Task Initialize()
        {
            if(!Directory.Exists(_dataDir))
                Directory.CreateDirectory(_dataDir);

            if (!File.Exists(_dataFile)) return;

            var json = await File.ReadAllTextAsync(_dataFile);

            if (string.IsNullOrWhiteSpace(json)) return;

            var subscriptions = JsonConvert.DeserializeObject<Dictionary<string, SubscriptionProperties>>(json);

            if (subscriptions == null || subscriptions.Keys.Count == 0) return;

            foreach (var kv in subscriptions)
            {
                this._subscriptions[kv.Key] = kv.Value;
            }
        }

        public Task<Immutable<IReadOnlyDictionary<string, SubscriptionProperties>>> GetSubscriptions() =>
            Task.FromResult(((IReadOnlyDictionary<string, SubscriptionProperties>)this._subscriptions).AsImmutable());

        public Task Dispatch(Immutable<EventGridEvent[]> events)
        {
            var eventGridEvents = events.Value;
            if (eventGridEvents?.Length == 0) return Task.CompletedTask;

            var tasks = new List<Task>();
            foreach (var @event in eventGridEvents)
            {
                tasks.Add(TryDispatch(@event));
            }

            return Task.WhenAll(tasks);
        }

        private Task TryDispatch(EventGridEvent @event)
        {
            var candidates = this._subscriptions.Where(kv => PredicateFilter(@event, ref kv)).ToList();

            var tasks = new List<Task>(candidates.Count);
            foreach (var item in candidates)
            {
                var stream = this._streamProvider.GetStream<Immutable<(string, SubscriptionProperties, EventGridEvent)>>(Guid.NewGuid(), Constants.WebHookDispatcherNamespace);
                tasks.Add(stream.OnNextAsync((item.Key, item.Value, @event).AsImmutable()));
            }

            return Task.WhenAll(tasks);
        }

        #region Predicates
        private static bool PredicateFilter(EventGridEvent @event, ref KeyValuePair<string, SubscriptionProperties> kv)
        {
            return MatchEventType(@event, ref kv) &&
                                SubjectStartWith(@event, ref kv) &&
                                SubjectEndsWith(@event, ref kv);
        }

        private static bool SubjectEndsWith(EventGridEvent @event, ref KeyValuePair<string, SubscriptionProperties> kv)
        {
            return (!string.IsNullOrWhiteSpace(kv.Value.Filter.SubjectEndsWith) || @event.Subject.EndsWith(kv.Value.Filter.SubjectEndsWith));
        }

        private static bool SubjectStartWith(EventGridEvent @event, ref KeyValuePair<string, SubscriptionProperties> kv)
        {
            return (!string.IsNullOrWhiteSpace(kv.Value.Filter.SubjectBeginsWith) || @event.Subject.StartsWith(kv.Value.Filter.SubjectBeginsWith));
        }

        private static bool MatchEventType(EventGridEvent @event, ref KeyValuePair<string, SubscriptionProperties> kv)
        {
            return kv.Value.Filter.IncludedEventTypes.Contains(@event.EventType) ||
                            kv.Value.Filter.IncludedEventTypes.Contains(ALL_EVENT_TYPES);
        } 
        #endregion
    }
}
