using Newtonsoft.Json;
using System;

namespace EventGridEmulator.Messages
{
    public class EndpointValidationData
    {
        [JsonProperty("validationCode")]
        public Guid ValidationCode { get; set; }

        [JsonProperty("validationUrl")]
        public string ValidationUrl { get; set; }
    }
}
