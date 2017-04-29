using Newtonsoft.Json;

namespace StrubT.IoT.Playground {

	class SenseHatEnvironment {

		[JsonProperty("temperature")]
		public double Temperature { get; set; }

		[JsonProperty("humidity")]
		public double Humidity { get; set; }

		[JsonProperty("pressure")]
		public double Pressure { get; set; }

		public override string ToString() => $"{nameof(Temperature)}={Temperature:0.00}, {nameof(Humidity)}={Humidity:0.00}, {nameof(Pressure)}={Pressure:#,##0.00}";

		internal class MqttConverter : Mqtt.StringConverter<SenseHatEnvironment> {

			public MqttConverter() : base(e => JsonConvert.SerializeObject(e), s => JsonConvert.DeserializeObject<SenseHatEnvironment>(s)) { }
		}
	}
}
