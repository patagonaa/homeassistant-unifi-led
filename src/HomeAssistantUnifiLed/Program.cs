using Microsoft.Extensions.Configuration;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;
using Newtonsoft.Json;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

            var devices = config.Devices.Select(x => new UnifiDevice { Config = x, Name = x.Name ?? x.Host, Effect = "white", State = false}).ToList();
            _devices = devices;

            var options = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(new MqttClientOptionsBuilder()
                    .WithTcpServer(mqttConfig.Host)
                    .WithCredentials(mqttConfig.Username, mqttConfig.Password)
                    .Build())
                .Build();

            var mqttClient = new MqttFactory().CreateManagedMqttClient();

            var filterBuilder = new MqttTopicFilterBuilder();
            foreach (var device in devices)
            {
                await mqttClient.SubscribeAsync($"homeassistant/light/{device.Name}/set");
                await mqttClient.SubscribeAsync($"homeassistant/light/{device.Name}/effect");
            }

            mqttClient.UseApplicationMessageReceivedHandler(MessageReceived);
            await mqttClient.StartAsync(options);

            foreach (var device in devices)
            {
                var lightConfig = new HomeAssistantDiscovery()
                {
                    UniqueId = device.Name,
                    Name = device.Name,
                    TopicBase = $"homeassistant/light/{device.Name}",
                    CommandTopic = "~/set",
                    EffectList = new List<string>
                    {
                        "white",
                        "blue"
                    },
                    EffectTopic = "~/effect"
                };

                var configJson = JsonConvert.SerializeObject(lightConfig);

                await mqttClient.PublishAsync($"homeassistant/light/{device.Name}/config", configJson, MqttQualityOfServiceLevel.ExactlyOnce);
                var deviceConfig = device.Config;
                device.SshClient = new SshClient(deviceConfig.Host, deviceConfig.Port, deviceConfig.Username, deviceConfig.Password);
            }

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
                }
                Thread.Sleep(10000);
            }
        }

        private static void MessageReceived(MqttApplicationMessageReceivedEventArgs messageEvent)
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

            Console.WriteLine($"AP {client.ConnectionInfo.Host} Color {valueToSend}");

            if (client.IsConnected)
            {
                using (var sc = client.CreateCommand($"echo '{valueToSend.ToString(CultureInfo.InvariantCulture)}' > /proc/gpio/led_pattern"))
                {
                    sc.Execute();
                }
            }
        }
    }
}
