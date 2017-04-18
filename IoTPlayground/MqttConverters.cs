using System;
using System.Text;
using Nmqtt;

namespace StrubT.IoT.Playground.Mqtt {

	abstract class MqttConverter<T> : IPublishDataConverter {

		public Func<T, byte[]> BinaryEncoder { get; }

		public Func<byte[], T> BinaryDecoder { get; }

		protected MqttConverter(Func<T, byte[]> encoder, Func<byte[], T> decoder) {

			BinaryEncoder = encoder;
			BinaryDecoder = decoder;
		}

		public byte[] BinaryEncode(T @object) => BinaryEncoder(@object);

		public T BinaryDecode(byte[] data) => BinaryDecoder(data);

		byte[] IPublishDataConverter.ConvertToBytes(object data) => data is T t ? BinaryEncode(t) : throw new ArgumentException($"Invalid type. Can only handle '{typeof(T)}'.", nameof(data));

		object IPublishDataConverter.ConvertFromBytes(byte[] messageData) => BinaryDecode(messageData);
	}

	abstract class StringConverter<T> : MqttConverter<T> {

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
}
