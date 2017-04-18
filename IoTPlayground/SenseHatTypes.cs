using Newtonsoft.Json;
using StrubT.IoT.Playground.Mqtt;

namespace StrubT.IoT.Playground {

	struct SenseHatEnvironment {

		[JsonProperty("temperature")]
		public double Temperature { get; set; }

		[JsonProperty("humidity")]
		public double Humidity { get; set; }

		[JsonProperty("pressure")]
		public double Pressure { get; set; }

		internal class Converter : StringConverter<SenseHatEnvironment> {

			public Converter() : base(e => JsonConvert.SerializeObject(e), s => JsonConvert.DeserializeObject<SenseHatEnvironment>(s)) { }
		}

		public override string ToString() => $"{nameof(Temperature)}={Temperature:0.00}, {nameof(Humidity)}={Humidity:0.00}, {nameof(Pressure)}={Pressure:#,##0.00}";
	}
}
