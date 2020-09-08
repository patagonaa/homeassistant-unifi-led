using System.Collections.Generic;

namespace HomeAssistantUnifiLed
{
    internal class Configuration
    {
        public MqttConfig Mqtt { get; set; }
        public IList<UnifiDeviceConfig> Devices { get; set; }
    }
}
