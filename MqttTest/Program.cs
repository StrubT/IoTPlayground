using System;
using Nmqtt;

namespace StrubT.IoT.Mqtt.Test {

	static class Program {

		static void Main() {

			var topic = "siot/DAT/F386-09CA-1F9F-9A1F-BAD3-F573-64A0-2A22/52AF61E9-CDE0-480F-A7C4-7E0F01D23C93";
			var clientId = "StrubT";

			using (var client = new MqttClient("siot.net", clientId)) {
				client.MessageAvailable += (sender, e) => Console.WriteLine($"{e.Message:0.00}");

				client.Connect();
				client.Subscribe<DoubleConverter>(topic, MqttQos.AtLeastOnce);

				Console.ReadLine();

				var r = new Random();
				for (var i = 0; i < 5; i++)
					client.PublishMessage<DoubleConverter>(topic, r.NextDouble() * 50 + 50);
			}
		}
	}
}
