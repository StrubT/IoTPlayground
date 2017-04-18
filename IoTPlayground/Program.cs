using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
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
		static void SenseHatEnvironment() {

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
				client.MessageAvailable += (sender, e) => Console.WriteLine($"{topicNames[e.Topic.Split('/').Last()],-20}: {e.Message:0.00}");

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
