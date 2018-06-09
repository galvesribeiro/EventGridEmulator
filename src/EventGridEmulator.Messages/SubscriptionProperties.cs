using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace EventGridEmulator.Messages
{
    public class SubscriptionProperties
    {
        [JsonProperty("destination")]
        public SubscriptionDestination Destination { get; set; }

        [JsonProperty("filter")]
        public SubscriptionFilter Filter { get; set; }

        [JsonProperty("labels")]
        public List<string> Labels { get; set; } = new List<string>();

        public void Validate()
        {
            if (this.Destination == null) throw new ArgumentNullException(nameof(this.Destination));
            if (this.Filter == null) throw new ArgumentNullException(nameof(this.Filter));
            if (this.Labels == null) throw new ArgumentNullException(nameof(this.Labels));

            this.Destination.Validate();
        }
    }
}
