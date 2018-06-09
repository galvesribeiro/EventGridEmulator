using EventGridEmulator.Messages;
using Orleans;
using Orleans.Concurrency;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventGridEmulator.Grains
{
    public interface ISubscriptionManagerGrain : IGrainWithGuidKey
    {
        Task Initialize();
        Task CreateSubscription(string name, CreateSubscriptionRequest request);
        Task Dispatch(Immutable<EventGridEvent[]> events);
        Task<Immutable<IReadOnlyDictionary<string, SubscriptionProperties>>> GetSubscriptions();
    }
}
