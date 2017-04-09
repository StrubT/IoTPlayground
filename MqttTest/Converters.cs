using System;
using System.Text;
using Nmqtt;

namespace StrubT.IoT.Mqtt.Test {

	abstract class PublishDataConverter<T> : IPublishDataConverter {

		public Func<T, byte[]> Encoder { get; }

		public Func<byte[], T> Decoder { get; }

		protected PublishDataConverter(Func<T, byte[]> encoder, Func<byte[], T> decoder) {

			Encoder = encoder;
			Decoder = decoder;
		}

		public byte[] Encode(T data) => Encoder(data);

		public T Decode(byte[] data) => Decoder(data);

		byte[] IPublishDataConverter.ConvertToBytes(object data) => data is T t ? Encode(t) : throw new ArgumentException($"Invalid type. Can only handle '{typeof(T)}'.", nameof(data));

		object IPublishDataConverter.ConvertFromBytes(byte[] messageData) => Decode(messageData);
	}

	class DoubleConverter : PublishDataConverter<double> {

		public static Encoding Encoding { get; } = Encoding.ASCII;

		public DoubleConverter() : base(d => Encoding.GetBytes(d.ToString()),
			b => double.Parse(Encoding.GetString(b))) { }
	}
}
