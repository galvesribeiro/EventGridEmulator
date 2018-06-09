using Newtonsoft.Json;
using System;

namespace EventGridEmulator.Messages
{
    public class CreateSubscriptionRequest
    {
        [JsonProperty("properties")]
        public SubscriptionProperties Properties { get; set; }

        public void Validate()
        {
            if (this.Properties == null) throw new ArgumentNullException(nameof(this.Properties));

            this.Properties.Validate();
        }
    }
}
