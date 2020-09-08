using Renci.SshNet;

namespace HomeAssistantUnifiLed
{
    class UnifiDevice
    {
        public string Name { get; set; }
        public bool State { get; set; }
        public string Effect { get; set; }
        public SshClient SshClient { get; set; }
        public UnifiDeviceConfig Config { get; set; }
    }
}
