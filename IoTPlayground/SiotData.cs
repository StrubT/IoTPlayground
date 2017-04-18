using System;
using System.Text;
using Newtonsoft.Json;
using Nmqtt;

namespace StrubT.IoT.Playground {

	abstract class PublishDataConverter<T> : IPublishDataConverter {

		public Func<T, byte[]> BinaryEncoder { get; }

		public Func<byte[], T> BinaryDecoder { get; }

		protected PublishDataConverter(Func<T, byte[]> encoder, Func<byte[], T> decoder) {

			BinaryEncoder = encoder;
			BinaryDecoder = decoder;
		}

		public byte[] BinaryEncode(T @object) => BinaryEncoder(@object);

		public T BinaryDecode(byte[] data) => BinaryDecoder(data);

		byte[] IPublishDataConverter.ConvertToBytes(object data) => data is T t ? BinaryEncode(t) : throw new ArgumentException($"Invalid type. Can only handle '{typeof(T)}'.", nameof(data));

		object IPublishDataConverter.ConvertFromBytes(byte[] messageData) => BinaryDecode(messageData);
	}

	abstract class StringConverter<T> : PublishDataConverter<T> {

		public static Encoding Encoding { get; } = Encoding.ASCII;

		public Func<T, string> StringEncoder { get; }

		public Func<string, T> StringDecoder { get; }

		protected StringConverter(Func<T, string> encoder, Func<string, T> decoder)
			: base(v => Encoding.GetBytes(encoder(v)), b => decoder(Encoding.GetString(b))) {

			StringEncoder = encoder;
			StringDecoder = decoder;
		}

		public string StringEncode(T @object) => StringEncoder(@object);

		public T StringDecode(string @string) => StringDecoder(@string);
	}

	class StringConverter : StringConverter<string> {

		public StringConverter() : base(s => s, s => s) { }
	}

	class DoubleConverter : StringConverter<double> {

		public DoubleConverter() : base(d => d.ToString(), s => double.Parse(s)) { }
	}

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
