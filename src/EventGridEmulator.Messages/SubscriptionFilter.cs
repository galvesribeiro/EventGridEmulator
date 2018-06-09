using Newtonsoft.Json;
using System.Collections.Generic;

namespace EventGridEmulator.Messages
{
    public class SubscriptionFilter
    {
        [JsonProperty("includedEventTypes")]
        public List<string> IncludedEventTypes { get; set; } = new List<string>();

        [JsonProperty("subjectBeginsWith")]
        public string SubjectBeginsWith { get; set; }

        [JsonProperty("subjectEndsWith")]
        public string SubjectEndsWith { get; set; }

        [JsonProperty("isSubjectCaseSensitive")]
        public bool IsSubjectCaseSensitive { get; set; }
    }
}
