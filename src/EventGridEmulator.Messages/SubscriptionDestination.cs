using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace EventGridEmulator.Messages
{
    public class SubscriptionDestination
    {
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("endpointType")]
        public EndpointType EndpointType { get; set; }

        [JsonProperty("properties")]
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        public void Validate()
        {
            if (this.EndpointType == EndpointType.Webhook)
            {
                if (!this.Properties.TryGetValue("endpointUrl", out string value) || string.IsNullOrWhiteSpace(value))
                    throw new ArgumentNullException("endpointUrl");
            }
        }
    }
}
