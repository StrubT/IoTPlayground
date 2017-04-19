using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nmqtt;

namespace StrubT.IoT.Playground {

	static class Program {

		static void Main() {

			var actions = (from m in typeof(Program).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
										 where m.GetCustomAttributes<ActionAttribute>().Any()
										 select (Action)Delegate.CreateDelegate(typeof(Action), m)).ToList();

			Console.WriteLine("ACTIONS:");
			for (var i = 0; i < actions.Count; i++)
				Console.WriteLine($"{i,3}: {actions[i].Method.Name}");
			Console.WriteLine();

			Action action;
			Console.Write("Choose action (enter #): ");
			while (!int.TryParse(Console.ReadLine(), out var i) || i < 0 || i >= actions.Count || (action = actions[i]) == null)
				Console.Write("Enter valid action number: ");

			Console.Title = action.Method.Name;

			var separator = new string('*', action.Method.Name.Length + 8);
			Console.WriteLine();
			Console.WriteLine(separator);
			Console.WriteLine($"*** {action.Method.Name} ***");
			Console.WriteLine(separator);
			Console.WriteLine();

			action();
		}

		[Action]
		static void SenseHatEnvironmentMqtt() {

			var clientId = "StrubT";

			var centerGuid = "F386-09CA-1F9F-9A1F-BAD3-F573-64A0-2A22";
			var combinedGuid = "B156D76BA28A425FB1626D1D9207BB4C";
			var temperatureGuid = "31B0E939B27C45229CB75A06B1E47919";
			var humidityGuid = "EB5E6260F83E4118A919C226CAC1E82B";
			var pressureGuid = "CF8EB59A961B4C58A075A822485708FB";

			var topicNames = new Dictionary<string, string> {
				[combinedGuid] = "Environment",
				[temperatureGuid] = "Temperature (°C)",
				[humidityGuid] = "Humidity (% rel)",
				[pressureGuid] = "Pressure (mbar)"
			};

			var combinedTopic = $"siot/DAT/{centerGuid}/{combinedGuid}";
			var temperatureTopic = $"siot/DAT/{centerGuid}/{temperatureGuid}";
			var humidityTopic = $"siot/DAT/{centerGuid}/{humidityGuid}";
			var pressureTopic = $"siot/DAT/{centerGuid}/{pressureGuid}";

			using (var client = new MqttClient("siot.net", clientId)) {
				client.MessageAvailable += (sender, e) => Console.WriteLine($"{topicNames[e.Topic.Split('/').Last()],-20}: {e.Message:#,##0.00}");

				client.Connect();
				client.Subscribe<SenseHatEnvironment.MqttConverter>(combinedTopic, MqttQos.AtLeastOnce);
				client.Subscribe<Mqtt.DoubleConverter>(temperatureTopic, MqttQos.AtLeastOnce);
				client.Subscribe<Mqtt.DoubleConverter>(humidityTopic, MqttQos.AtLeastOnce);
				client.Subscribe<Mqtt.DoubleConverter>(pressureTopic, MqttQos.AtLeastOnce);

				Console.ReadLine();

				var r = new Random();
				for (var i = 0; i < 5; i++) {
					var data = new SenseHatEnvironment { Temperature = r.NextDouble() * 20 + 20, Humidity = r.NextDouble() * 20 + 20, Pressure = 1000 + (r.NextDouble() - 0.5) * 100 };
					client.PublishMessage<SenseHatEnvironment.MqttConverter>(combinedTopic, data);
					client.PublishMessage<Mqtt.DoubleConverter>(temperatureTopic, data.Temperature);
					client.PublishMessage<Mqtt.DoubleConverter>(humidityTopic, data.Humidity);
					client.PublishMessage<Mqtt.DoubleConverter>(pressureTopic, data.Pressure);
				}

				Thread.Sleep(250);
			}
		}

		[Action]
		static void SenseHatEnvironmentRest() {

			var centerGuid = "F386-09CA-1F9F-9A1F-BAD3-F573-64A0-2A22";
			var combinedGuid = "B156D76BA28A425FB1626D1D9207BB4C";
			var temperatureGuid = "31B0E939B27C45229CB75A06B1E47919";
			var humidityGuid = "EB5E6260F83E4118A919C226CAC1E82B";
			var pressureGuid = "CF8EB59A961B4C58A075A822485708FB";

			using (var client = new WebClient()) {

				var centerUrl = $"http://url.siot.net/?licence={centerGuid}";
				var centerInfo = JsonConvert.DeserializeObject<SiotCenter>(client.DownloadString(centerUrl));

				Console.WriteLine($"[{centerInfo.Guid}] '{centerInfo.Name}'");
				Console.WriteLine($"{centerInfo.Url} (port: {centerInfo.Port}, websocket: {centerInfo.WebSocketPort})");

				PrintSensorInformation<SenseHatEnvironment>("Environment", combinedGuid);
				PrintSensorInformation<double>("Temperature (°C)", temperatureGuid);
				PrintSensorInformation<double>("Humidity (% rel)", humidityGuid);
				PrintSensorInformation<double>("Pressure (mbar)", pressureGuid);

				void PrintSensorInformation<TValue>(string sensorName, string sensorGuid)
				{
					var manifestUrl = $"https://siot.net:11805/getmanifest?sensorUID={sensorGuid}";
					var configurationUrl = $"https://siot.net:11805/getconfig?sensorUID={sensorGuid}";
					var dataUrl = $"https://siot.net:11805/getdata?centerUID={centerGuid}&sensorUID={sensorGuid}";
					var dataHistoryUrl = $"https://siot.net:11805/getdatalastn?centerUID={centerGuid}&sensorUID={sensorGuid}&count=15";

					var sensorManifest = JsonConvert.DeserializeObject<SiotSensorActorManifest>(client.DownloadString(manifestUrl));
					var sensorConfiguration = JsonConvert.DeserializeObject<SiotCenterConfiguration>(client.DownloadString(configurationUrl));
					var sensorData = JsonConvert.DeserializeObject<TValue>(client.DownloadString(dataUrl));
					var sensorDataHistory = JsonConvert.DeserializeObject<SiotHistoryValue<TValue>[]>(client.DownloadString(dataHistoryUrl));

					Console.WriteLine();
					Console.WriteLine($"*** {sensorName} ***");
					Console.WriteLine($"[{sensorGuid}] '{sensorManifest.Name}' ({sensorManifest.Description})");
					Console.WriteLine($"zone: [{sensorManifest.Zone.Guid}] '{sensorManifest.Zone.Name}'");
					Console.WriteLine($"type: {sensorManifest.Type}, value type: {sensorManifest.ValueType}, storage: {sensorConfiguration.Storage}");
					if (sensorManifest.JsonMapping is JObject j)
						Console.WriteLine($"JSON mapping: {string.Join(", ", j.Properties().Select(p => $"[{p.Value}] '{p.Name}'"))}");
					Console.WriteLine($"latest data value: {sensorData:#,##0.00}");
					foreach (var value in sensorDataHistory.Select((v, i) => (Index: i, Data: v.Data, DateTime: v.DateTime)))
						Console.WriteLine($"{value.Index,2}: {value.Data,7:#,##0.00} ({value.DateTime})");
				}
			}
		}

		[Action]
		static void SenseHatMessage() {

			var clientId = "StrubT";

			var centerGuid = "F386-09CA-1F9F-9A1F-BAD3-F573-64A0-2A22";
			var messageGuid = "14760cc8-4cea-c63c-a825-cef162c16146";
			var messageTopic = $"siot/DAT/{centerGuid}/{messageGuid}";

			Console.Write("Enter message: ");
			var msg = Console.ReadLine();

			using (var client = new MqttClient("siot.net", clientId)) {
				client.Connect();

				client.PublishMessage<Mqtt.StringConverter>(messageTopic, msg);
			}
		}

		[Action]
		static void SenseHatLedArray() {

			var clientId = "StrubT";

			var centerGuid = "F386-09CA-1F9F-9A1F-BAD3-F573-64A0-2A22";
			var messageGuid = "14760cc8-4cea-c63c-a825-cef162c16146";
			var messageTopic = $"siot/DAT/{centerGuid}/{messageGuid}";

			var leds = new Color[8, 8];
			leds[0, 0] = Color.Red;
			leds[2, 7] = Color.BlueViolet;
			leds[5, 5] = Color.Navy;
			leds[4, 3] = Color.PeachPuff;

			var rng = Enumerable.Range(0, 8).ToList();

			using (var client = new MqttClient("siot.net", clientId)) {
				client.Connect();

				var msg = rng.SelectMany(x => rng.SelectMany(y => new[] { x, y, leds[x, y].R, leds[x, y].G, leds[x, y].B })).ToList();
				client.PublishMessage<Mqtt.StringConverter>(messageTopic, string.Join(",", msg));
			}
		}

		[Action]
		static void SenseHatImages8x8() {

			var clientId = "StrubT";

			var centerGuid = "F386-09CA-1F9F-9A1F-BAD3-F573-64A0-2A22";
			var messageGuid = "14760cc8-4cea-c63c-a825-cef162c16146";
			var messageTopic = $"siot/DAT/{centerGuid}/{messageGuid}";

			var rng = Enumerable.Range(0, 8).ToList();

			using (var client = new MqttClient("siot.net", clientId)) {
				client.Connect();

				foreach (var img in new[] { Properties.Resources.linkedin, Properties.Resources.twitter, Properties.Resources.facebook })
					using (img) {

						var msg = rng.SelectMany(x => rng.SelectMany(y => {
							var pxl = img.GetPixel(x, y);
							return new[] { x, y, pxl.R, pxl.G, pxl.B };
						})).ToList();
						client.PublishMessage<Mqtt.StringConverter>(messageTopic, string.Join(",", msg));

						Thread.Sleep(1500);
					}
			}
		}

		[Action]
		static void SenseHatImageSet() {

			var clientId = "StrubT";

			var centerGuid = "F386-09CA-1F9F-9A1F-BAD3-F573-64A0-2A22";
			var messageGuid = "14760cc8-4cea-c63c-a825-cef162c16146";
			var messageTopic = $"siot/DAT/{centerGuid}/{messageGuid}";

			var rng = Enumerable.Range(0, 8).ToList();

			var borderX = 14;
			var borderY = 12;
			var margin = 10;
			var size = 8;

			using (var img = Properties.Resources.set)
			using (var client = new MqttClient("siot.net", clientId)) {
				client.Connect();

				for (var Y = 0; Y < 16; Y++)
					for (var X = 0; X < 21; X++) {
						Console.Write($"{X}x{Y}    \r");

						var msg = rng.SelectMany(x => rng.SelectMany(y => {
							var pxl = img.GetPixel(borderX + X * (size + margin) + x, borderY + Y * (size + margin) + y);
							return new[] { x, y, pxl.R, pxl.G, pxl.B };
						})).ToList();
						client.PublishMessage<Mqtt.StringConverter>(messageTopic, string.Join(",", msg));

						Thread.Sleep(250);
					}
			}

			Console.WriteLine();
		}
	}

	[AttributeUsage(AttributeTargets.Method)]
	class ActionAttribute : Attribute { }
}
