using Newtonsoft.Json;
using System;

namespace EventGridEmulator.Messages
{
    public class EventGridEvent
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("topic")]
        public string Topic { get; set; }

        [JsonProperty("subject")]
        public string Subject { get; set; } = "";

        [JsonProperty("data")]
        public object Data { get; set; }

        [JsonProperty("eventType")]
        public string EventType { get; set; }

        [JsonProperty("eventTime")]
        public DateTime EventTime { get; set; }

        [JsonProperty("metadataVersion")]
        public string MetadataVersion { get; set; } = "1";

        [JsonProperty("dataVersion")]
        public string DataVersion { get; set; } = "2";
    }
}
