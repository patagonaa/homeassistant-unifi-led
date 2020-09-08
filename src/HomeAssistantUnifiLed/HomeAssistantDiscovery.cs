using Newtonsoft.Json;
using System.Collections.Generic;

namespace HomeAssistantUnifiLed
{
    class HomeAssistantDiscovery
    {
        [JsonProperty("unique_id")]
        public string UniqueId { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("~")]
        public string TopicBase { get; set; }
        [JsonProperty("command_topic")]
        public string CommandTopic { get; set; }
        [JsonProperty("effect_list")]
        public IList<string> EffectList { get; set; }
        [JsonProperty("effect_command_topic")]
        public string EffectTopic { get; set; }
    }
}
