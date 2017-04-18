using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json;
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
			var temperatureGuid = "31B0E939B27C45229CB75A06B1E47919";
			var humidityGuid = "EB5E6260F83E4118A919C226CAC1E82B";
			var pressureGuid = "CF8EB59A961B4C58A075A822485708FB";

			var topicNames = new Dictionary<string, string> {
				[temperatureGuid] = "Temperature (°C)",
				[humidityGuid] = "Humidity (% rel)",
				[pressureGuid] = "Pressure (mbar)"
			};

			var temperatureTopic = $"siot/DAT/{centerGuid}/{temperatureGuid}";
			var humidityTopic = $"siot/DAT/{centerGuid}/{humidityGuid}";
			var pressureTopic = $"siot/DAT/{centerGuid}/{pressureGuid}";

			using (var client = new MqttClient("siot.net", clientId)) {
				client.MessageAvailable += (sender, e) => Console.WriteLine($"{topicNames[e.Topic.Split('/').Last()],-20}: {e.Message:#,##0.00}");

				client.Connect();
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

				Thread.Sleep(250);
			}
		}

		[Action]
		static void SenseHatEnvironmentRest() {

			var centerGuid = "F386-09CA-1F9F-9A1F-BAD3-F573-64A0-2A22";
			var temperatureGuid = "31B0E939B27C45229CB75A06B1E47919";
			var humidityGuid = "EB5E6260F83E4118A919C226CAC1E82B";
			var pressureGuid = "CF8EB59A961B4C58A075A822485708FB";

			var historyCount = 25;

			var centerUrl = $"http://url.siot.net/?licence={centerGuid}";
			var temperatureManifestUrl = $"https://siot.net:11805/getmanifest?sensorUID={temperatureGuid}";
			var temperatureConfigurationUrl = $"https://siot.net:11805/getconfig?sensorUID={temperatureGuid}";
			var temperatureDataUrl = $"https://siot.net:11805/getdata?centerUID={centerGuid}&sensorUID={temperatureGuid}";
			var temperatureDataHistoryUrl = $"https://siot.net:11805/getdatalastn?centerUID={centerGuid}&sensorUID={temperatureGuid}&count={historyCount}";
			var humidityManifestUrl = $"https://siot.net:11805/getmanifest?sensorUID={humidityGuid}";
			var humidityConfigurationUrl = $"https://siot.net:11805/getconfig?sensorUID={humidityGuid}";
			var humidityDataUrl = $"https://siot.net:11805/getdata?centerUID={centerGuid}&sensorUID={humidityGuid}";
			var humidityDataHistoryUrl = $"https://siot.net:11805/getdatalastn?centerUID={centerGuid}&sensorUID={humidityGuid}&count={historyCount}";
			var pressureManifestUrl = $"https://siot.net:11805/getmanifest?sensorUID={pressureGuid}";
			var pressureConfigurationUrl = $"https://siot.net:11805/getconfig?sensorUID={pressureGuid}";
			var pressureDataUrl = $"https://siot.net:11805/getdata?centerUID={centerGuid}&sensorUID={pressureGuid}";
			var pressureDataHistoryUrl = $"https://siot.net:11805/getdatalastn?centerUID={centerGuid}&sensorUID={pressureGuid}&count={historyCount}";

			var dtBase = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

			using (var client = new WebClient()) {

				var centerInfo = JsonConvert.DeserializeAnonymousType(client.DownloadString(centerUrl), new { licence = string.Empty, name = string.Empty, url = string.Empty, port = 0, webSocketPort = 0 });

				Console.WriteLine($"[{centerInfo.licence}] '{centerInfo.name}'");
				Console.WriteLine($"{centerInfo.url} (port: {centerInfo.port}, websocket: {centerInfo.webSocketPort})");

				foreach (var sensor in new[] {
					new { Name = "Temperature (°C)", Guid = temperatureGuid, ManifestUrl = temperatureManifestUrl, ConfigurationUrl = temperatureConfigurationUrl, DataUrl = temperatureDataUrl, DataHistoryUrl = temperatureDataHistoryUrl },
					new { Name = "Humidity (% rel)", Guid = humidityGuid, ManifestUrl = humidityManifestUrl, ConfigurationUrl = humidityConfigurationUrl, DataUrl = humidityDataUrl, DataHistoryUrl = humidityDataHistoryUrl },
					new { Name = "Pressure (mbar)", Guid = pressureGuid, ManifestUrl = pressureManifestUrl, ConfigurationUrl = pressureConfigurationUrl, DataUrl = pressureDataUrl, DataHistoryUrl = pressureDataHistoryUrl }
				}) {

					var sensorManifest = JsonConvert.DeserializeAnonymousType(
						client.DownloadString(sensor.ManifestUrl),
						new {
							name = string.Empty,
							type = string.Empty,
							zone = new { name = string.Empty, guid = string.Empty },
							description = string.Empty,
							valueType = string.Empty/*,
							jsonMapping = new object(),
							file = new { name = string.Empty, type = string.Empty, size = 0L, date = 0L }*/
						});
					var sensorConfiguration = JsonConvert.DeserializeAnonymousType(client.DownloadString(sensor.ConfigurationUrl), new { storage = string.Empty });
					var sensorData = JsonConvert.DeserializeAnonymousType(client.DownloadString(sensor.DataUrl), 0.0);
					var sensorDataHistory = JsonConvert.DeserializeAnonymousType(client.DownloadString(sensor.DataHistoryUrl), new[] { new { data = 0.0, time = 0L } });

					Console.WriteLine();
					Console.WriteLine($"*** {sensor.Name} ***");
					Console.WriteLine($"[{sensor.Guid}] '{sensorManifest.name}' ({sensorManifest.description})");
					Console.WriteLine($"zone: [{sensorManifest.zone.guid}] '{sensorManifest.zone.name}'");
					Console.WriteLine($"type: {sensorManifest.type}, value type: {sensorManifest.valueType}, storage: {sensorConfiguration.storage}");
					Console.WriteLine($"latest data value: {sensorData:#,##0.00}");
					foreach (var value in sensorDataHistory.Select((v, i) => new { Index = i, Data = v.data, DateTime = dtBase.AddMilliseconds(v.time).ToLocalTime() }))
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

				client.PublishMessage<StringConverter>(messageTopic, msg);
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
				client.PublishMessage<StringConverter>(messageTopic, string.Join(",", msg));
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
						client.PublishMessage<StringConverter>(messageTopic, string.Join(",", msg));

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
						client.PublishMessage<StringConverter>(messageTopic, string.Join(",", msg));

						Thread.Sleep(250);
					}
			}

			Console.WriteLine();
		}
	}

	[AttributeUsage(AttributeTargets.Method)]
	class ActionAttribute : Attribute { }
}
