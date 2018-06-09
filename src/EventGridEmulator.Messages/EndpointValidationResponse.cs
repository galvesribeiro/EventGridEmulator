using Newtonsoft.Json;
using System;

namespace EventGridEmulator.Messages
{
    public class EndpointValidationResponse
    {
        [JsonProperty("validationResponse")]
        public Guid ValidationResponse { get; set; }
    }
}
