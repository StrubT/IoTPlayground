using System;
using System.Text;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace StrubT.IoT.Mqtt.Test {

	static class Program {

		static void Main() {

			var centerGuid = "F386-09CA-1F9F-9A1F-BAD3-F573-64A0-2A22";
			var sensorGuid = "52AF61E9-CDE0-480F-A7C4-7E0F01D23C93";
			var clientGuid = Guid.NewGuid();

			var client = new MqttClient("siot.net");
			client.MqttMsgPublishReceived += (sender, e) => Console.WriteLine($"{double.Parse(Encoding.ASCII.GetString(e.Message)):0.00}");

			client.Connect(clientGuid.ToString());
			client.Subscribe(new[] { $"siot/DAT/{centerGuid}/#" }, new[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });

			Console.ReadLine();

			var r = new Random();
			for (var i = 0; i < 5; i++)
				client.Publish($"siot/DAT/{centerGuid}/{sensorGuid}", Encoding.ASCII.GetBytes((r.NextDouble() * 50 + 50).ToString()));

			client.Disconnect();
		}
	}
}
