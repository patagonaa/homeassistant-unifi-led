using HomeAssistantDiscoveryHelper;
using Microsoft.Extensions.Configuration;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;
using Newtonsoft.Json;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HomeAssistantUnifiLed
{
    class Program
    {
        private static IList<UnifiDevice> _devices = new List<UnifiDevice>();

        static async Task Main(string[] args)
        {
            var configRoot = new ConfigurationBuilder()
                .AddJsonFile("config.json")
                .Build();

            var config = configRoot.Get<Configuration>();
            var mqttConfig = config.Mqtt;

            var devices = config.Devices.Select(x => new UnifiDevice { Config = x, Name = x.Name ?? x.Host, Effect = "white", State = false }).ToList();
            _devices = devices;

            var options = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(new MqttClientOptionsBuilder()
                    .WithTcpServer(mqttConfig.Host)
                    .WithCredentials(mqttConfig.Username, mqttConfig.Password)
                    .WithWillTopic("homeassistantunifiled/bridge/state")
                    .WithWillPayload(Encoding.UTF8.GetBytes("offline"))
                    .WithWillRetain(true)
                    .Build())
                .Build();

            var mqttClient = new MqttFactory().CreateManagedMqttClient();

            var filterBuilder = new MqttTopicFilterBuilder();
            foreach (var device in devices)
            {
                var deviceConfig = device.Config;
                device.SshClient = new SshClient(deviceConfig.Host, deviceConfig.Port, deviceConfig.Username, deviceConfig.Password);
                device.SshClient.Connect();
                Console.WriteLine($"Connected to {deviceConfig.Host}");

                await mqttClient.SubscribeAsync($"homeassistant/light/{device.Name}/set");
                await mqttClient.SubscribeAsync($"homeassistant/light/{device.Name}/effect");
            }

            mqttClient.ApplicationMessageReceivedAsync += MessageReceived;
            await mqttClient.StartAsync(options);

            await mqttClient.EnqueueAsync($"homeassistantunifiled/bridge/state", "online", MqttQualityOfServiceLevel.ExactlyOnce, true);

            foreach (var device in devices)
            {
                var lightConfig = new HomeAssistantDiscovery()
                {
                    UniqueId = device.Name,
                    Name = null,
                    Device = new HomeAssistantDevice
                    {
                        Name = device.Name,
                        Identifiers = new List<string>
                        {
                            device.Name,
                            device.Config.Host
                        }
                    },
                    TopicBase = $"homeassistant/light/{device.Name}",
                    CommandTopic = "~/set",
                    EffectList = new List<string>
                    {
                        "white",
                        "blue"
                    },
                    EffectTopic = "~/effect",
                    AvailabilityTopic = "homeassistantunifiled/bridge/state",
                    Retain = true
                };

                var configJson = JsonConvert.SerializeObject(lightConfig);

                await mqttClient.EnqueueAsync($"homeassistant/light/{device.Name}/config", configJson, MqttQualityOfServiceLevel.ExactlyOnce, true);
            }

            int i = 0;

            while (true)
            {
                foreach (var device in _devices)
                {
                    var client = device.SshClient;
                    if (!client.IsConnected)
                    {
                        client.Connect();
                        Console.WriteLine($"Connected to {client.ConnectionInfo.Host}");
                    }
                    if (i % 6 == 0)
                    {
                        SetDeviceState(device);
                    }
                }
                Thread.Sleep(10000);
                i++;
            }
        }

        private static Task MessageReceived(MqttApplicationMessageReceivedEventArgs messageEvent)
        {
            var message = messageEvent.ApplicationMessage;
            var splitTopic = message.Topic.Split('/');
            var name = splitTopic[2];
            var type = splitTopic[3];

            var device = _devices.FirstOrDefault(x => x.Name == name);

            switch (type)
            {
                case "set":
                    device.State = message.ConvertPayloadToString().Equals("ON", StringComparison.OrdinalIgnoreCase);
                    break;
                case "effect":
                    device.Effect = message.ConvertPayloadToString();
                    break;
                default:
                    break;
            }

            Console.WriteLine($"AP {device.Config.Host} Color {device.Effect} {(device.State ? "ON" : "OFF")}");

            SetDeviceState(device);
            return Task.CompletedTask;
        }

        private static void SetDeviceState(UnifiDevice device)
        {
            var client = device.SshClient;

            var valueToSend = 0;
            if (device.State)
            {
                if (device.Effect == "blue")
                {
                    valueToSend |= 1;
                }
                if (device.Effect == "white")
                {
                    valueToSend |= 2;
                }
            }

            if (client.IsConnected)
            {
                using (var sc = client.CreateCommand("sed -i '/mgmt.led_pattern_override/d' /var/etc/persistent/cfg/mgmt && " +
                    $"echo 'mgmt.led_pattern_override={valueToSend.ToString(CultureInfo.InvariantCulture)}' >> /var/etc/persistent/cfg/mgmt && " +
                    $"echo '{valueToSend.ToString(CultureInfo.InvariantCulture)}' > /proc/gpio/led_pattern"))
                {
                    sc.Execute();
                }
            }
        }
    }
}
