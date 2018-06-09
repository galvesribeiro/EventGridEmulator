using EventGridEmulator.Messages;
using Orleans;
using System.Threading.Tasks;

namespace EventGridEmulator.Grains
{
    public interface IEventDispatcherGrain : IGrainWithGuidKey
    {
        Task Dispatch((string, SubscriptionProperties, EventGridEvent) eventGridEvent);
    }
}
