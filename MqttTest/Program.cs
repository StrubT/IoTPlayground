using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Nmqtt;

namespace StrubT.IoT.Mqtt.Test {

	static class Program {

		static void Main() {

			var clientId = "StrubT";

			var centerGuid = "F386-09CA-1F9F-9A1F-BAD3-F573-64A0-2A22";
			var temperatureGuid = "31B0E939B27C45229CB75A06B1E47919";
			var humidityGuid = "EB5E6260F83E4118A919C226CAC1E82B";
			var pressureGuid = "CF8EB59A961B4C58A075A822485708FB";
			var messageGuid = "14760cc8-4cea-c63c-a825-cef162c16146";

			var topicNames = new Dictionary<string, string> {
				[temperatureGuid] = "Temperature (°C)",
				[humidityGuid] = "Humidity (% rel)",
				[pressureGuid] = "Pressure (mbar)",
				[messageGuid] = "Sense HAT Message"
			};

			var temperatureTopic = $"siot/DAT/{centerGuid}/{temperatureGuid}";
			var humidityTopic = $"siot/DAT/{centerGuid}/{humidityGuid}";
			var pressureTopic = $"siot/DAT/{centerGuid}/{pressureGuid}";
			var messageTopic = $"siot/DAT/{centerGuid}/{messageGuid}";

			using (var client = new MqttClient("siot.net", clientId)) {
				client.MessageAvailable += (sender, e) => Console.WriteLine($"{topicNames[e.Topic.Split('/').Last()],-20}: {e.Message:0.00}");

				client.Connect();
				client.PublishMessage<StringConverter>(messageTopic, "Hi");

				client.Subscribe<DoubleConverter>(temperatureTopic, MqttQos.AtLeastOnce);
				client.Subscribe<DoubleConverter>(humidityTopic, MqttQos.AtLeastOnce);
				client.Subscribe<DoubleConverter>(pressureTopic, MqttQos.AtLeastOnce);

				Console.ReadLine();

				var r = new Random();
				for (var i = 0; i < 5; i++) {
					client.PublishMessage<DoubleConverter>(temperatureTopic, r.NextDouble() * 20 + 20);
					client.PublishMessage<DoubleConverter>(humidityTopic, r.NextDouble() * 20 + 20);
					client.PublishMessage<DoubleConverter>(pressureTopic, 1000 + (r.NextDouble() - 0.5) * 100);
				}

				client.PublishMessage<StringConverter>(messageTopic, "Bye");
				Thread.Sleep(250);
			}
		}
	}
}
