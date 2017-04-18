using System;
using System.Text;
using Nmqtt;

namespace StrubT.IoT.Mqtt.Test {

	abstract class PublishDataConverter<T> : IPublishDataConverter {

		public Func<T, byte[]> BinaryEncoder { get; }

		public Func<byte[], T> BinaryDecoder { get; }

		protected PublishDataConverter(Func<T, byte[]> encoder, Func<byte[], T> decoder) {

			BinaryEncoder = encoder;
			BinaryDecoder = decoder;
		}

		public byte[] Encode(T data) => BinaryEncoder(data);

		public T Decode(byte[] data) => BinaryDecoder(data);

		byte[] IPublishDataConverter.ConvertToBytes(object data) => data is T t ? Encode(t) : throw new ArgumentException($"Invalid type. Can only handle '{typeof(T)}'.", nameof(data));

		object IPublishDataConverter.ConvertFromBytes(byte[] messageData) => Decode(messageData);
	}

	abstract class StringConverterBase<T> : PublishDataConverter<T> {

		public static Encoding Encoding { get; } = Encoding.ASCII;

		public Func<T, string> StringEncoder { get; }

		public Func<string, T> StringDecoder { get; }

		protected StringConverterBase(Func<T, string> encoder, Func<string, T> decoder)
			: base(v => Encoding.GetBytes(encoder(v)), b => decoder(Encoding.GetString(b))) {

			StringEncoder = encoder;
			StringDecoder = decoder;
		}
	}

	class StringConverter : StringConverterBase<string> {

		public StringConverter() : base(s => s, s => s) { }
	}

	class DoubleConverter : StringConverterBase<double> {

		public DoubleConverter() : base(d => d.ToString(), s => double.Parse(s)) { }
	}
}
