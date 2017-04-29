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
			//var combinedTopicTrigger = $"siot/TRG/{centerGuid}/{combinedGuid}";
			//var combinedTopicCommand = $"siot/CMD/{centerGuid}/{combinedGuid}";
			//var combinedTopicStatus = $"siot/STA/{centerGuid}/{combinedGuid}";

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

			//var monday = DateTime.Today.AddDays((DateTime.Today.DayOfWeek - DayOfWeek.Monday + 7) % 7);
			//var lastMonday = monday.AddDays(-7);

			using (var client = new WebClient()) {

				var centerUrl = $"http://url.siot.net/?licence={centerGuid}";
				//var inputUrl = $"https://siot.net:11805/getinput?centerUID={centerGuid}";

				var centerInfo = JsonConvert.DeserializeObject<SiotCenter>(client.DownloadString(centerUrl));

				Console.WriteLine($"[{centerInfo.Guid}] '{centerInfo.Name}'");
				Console.WriteLine($"{centerInfo.Url} (port: {centerInfo.Port}, websocket: {centerInfo.WebSocketPort})");

				var r = new Random();
				var environmentData = Enumerable.Range(0, 5)
					.Select(i => new SenseHatEnvironment { Temperature = r.NextDouble() * 20 + 20, Humidity = r.NextDouble() * 20 + 20, Pressure = 1000 + (r.NextDouble() - 0.5) * 100 })
					.ToList();

				PrintSensorInformation<SenseHatEnvironment>("Environment", combinedGuid, environmentData);
				PrintSensorInformation<double>("Temperature (°C)", temperatureGuid, environmentData.Select(d => d.Temperature));
				PrintSensorInformation<double>("Humidity (% rel)", humidityGuid, environmentData.Select(d => d.Humidity));
				PrintSensorInformation<double>("Pressure (mbar)", pressureGuid, environmentData.Select(d => d.Pressure));

				void PrintSensorInformation<TValue>(string sensorName, string sensorGuid, IEnumerable<TValue> data)
				{
					var manifestUrl = $"https://siot.net:11805/getmanifest?sensorUID={sensorGuid}";
					var configurationUrl = $"https://siot.net:11805/getconfig?sensorUID={sensorGuid}";
					var dataUrl = $"https://siot.net:11805/getdata?centerUID={centerGuid}&sensorUID={sensorGuid}";
					var dataHistoryUrl = $"https://siot.net:11805/getdatalastn?centerUID={centerGuid}&sensorUID={sensorGuid}&count=15";
					//var dataHistoryFromUrl = $"https://siot.net:11805/getdatalastn?centerUID={centerGuid}&sensorUID={sensorGuid}&from={Json.SiotDateTimeConverter.Encode(monday)}";
					//var dataHistoryFromToUrl = $"https://siot.net:11805/getdatalastn?centerUID={centerGuid}&sensorUID={sensorGuid}&from={Json.SiotDateTimeConverter.Encode(lastMonday)}&to={Json.SiotDateTimeConverter.Encode(monday)}";

					var sensorManifest = JsonConvert.DeserializeObject<SiotSensorActorManifest>(client.DownloadString(manifestUrl));
					var sensorConfiguration = JsonConvert.DeserializeObject<SiotCenterConfiguration>(client.DownloadString(configurationUrl));
					var sensorData = JsonConvert.DeserializeObject<TValue>(client.DownloadString(dataUrl));
					var sensorDataHistory = JsonConvert.DeserializeObject<SiotHistoryValue<TValue>[]>(client.DownloadString(dataHistoryUrl));
					//var sensorDataFromHistory = JsonConvert.DeserializeObject<SiotHistoryValue<TValue>[]>(client.DownloadString(dataHistoryFromUrl));
					//var sensorDataFromToHistory = JsonConvert.DeserializeObject<SiotHistoryValue<TValue>[]>(client.DownloadString(dataHistoryFromToUrl));

					Console.WriteLine();
					Console.WriteLine($"*** {sensorName} ***");
					Console.WriteLine($"[{sensorGuid}] '{sensorManifest.Name}' ({sensorManifest.Description})");
					Console.WriteLine($"zone: [{sensorManifest.Zone.Guid}] '{sensorManifest.Zone.Name}'");
					Console.WriteLine($"type: {sensorManifest.Type}, value type: {sensorManifest.ValueType}, storage: {sensorConfiguration.Storage}");
					if (sensorManifest.JsonMapping is JObject j)
						Console.WriteLine($"JSON mapping: {string.Join(", ", j.Properties().Select(p => $"[{p.Value}] '{p.Name}'"))}");
					Console.WriteLine($"latest data value: {sensorData:#,##0.00}");
					foreach (var value in sensorDataHistory.Select((v, i) => (Index: i, Data: v.Data, DateTime: v.DateTime)))
						Console.WriteLine($"{value.Index,2}: {value.Data,8:#,##0.00} ({value.DateTime})");
					//Console.WriteLine($"{sensorDataFromHistory.Length} data values recorded this week ({sensorDataFromHistory.First().DateTime:g} to {sensorDataFromHistory.Last().DateTime:g})");
					//Console.WriteLine($"{sensorDataFromToHistory.Length} data values recorded last week ({sensorDataFromToHistory.First().DateTime:g} to {sensorDataFromToHistory.Last().DateTime:g})");

					//var mqttGetUrl = $"https://siot.net:11805/get/{sensorGuid}";

					//var mqttData = client.DownloadString(mqttGetUrl);
					//Console.WriteLine(mqttData);

					//foreach (var element in data) {
					//	var mqttSetUrl = $"https://siot.net:11805/set/{sensorGuid}?value={element}";

					//	var mqttResult = client.DownloadString(mqttSetUrl);
					//	Console.WriteLine(mqttResult);
					//}
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

		[Action]
		static void EncodeMorseCode() {

			var codes = new int[,] {
				/*A*/ { 1, 3, 0, 0 }, /*B*/ { 3, 1, 1, 1 }, /*C*/ { 3, 1, 3, 1 }, /*D*/ { 3, 1, 1, 0 }, /*E*/ { 1, 0, 0, 0 }, /*F*/ { 1, 1, 3, 1 }, /*G*/ { 3, 3, 1, 0 }, /*H*/ { 1, 1, 1, 1 },
				/*I*/ { 1, 1, 0, 0 }, /*J*/ { 1, 3, 3, 3 }, /*K*/ { 3, 1, 3, 0 }, /*L*/ { 1, 3, 1, 1 }, /*M*/ { 3, 3, 0, 0 }, /*N*/ { 3, 1, 0, 0 }, /*O*/ { 3, 3, 3, 0 }, /*P*/ { 1, 3, 3, 1 },
				/*Q*/ { 3, 3, 1, 3 }, /*R*/ { 1, 3, 1, 0 }, /*S*/ { 1, 1, 1, 0 }, /*T*/ { 3, 0, 0, 0 }, /*U*/ { 1, 1, 3, 0 }, /*V*/ { 1, 1, 1, 3 }, /*W*/ { 1, 3, 3, 0 }, /*X*/ { 3, 1, 1, 3 },
				/*Y*/ { 3, 1, 3, 3 }, /*Z*/ { 3, 3, 1, 1 }
			};

			var encoded = new byte[codes.GetLength(0)];
			for (var i = 0; i < codes.GetLength(0); i++) {
				ref var e = ref encoded[i];
				for (var j = 0; j < codes.GetLength(1); j++) {
					if (j > 0) e <<= 2;
					e |= (byte)codes[i, j];
				}

				var a = (e & 0b11000000) >> 6;
				var b = (e & 0b00110000) >> 4;
				var c = (e & 0b00001100) >> 2;
				var d = e & 0b00000011;

				Console.WriteLine($"{(char)('A' + i)}: 0x{e:X2} ({a}, {b}, {c}, {d})");
			}
		}
	}

	[AttributeUsage(AttributeTargets.Method)]
	class ActionAttribute : Attribute { }
}
