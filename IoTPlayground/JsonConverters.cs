using System;
using Newtonsoft.Json;

namespace StrubT.IoT.Playground.Json {

	class SiotDateTimeConverter : JsonConverter {

		static DateTime BaseTimestamp { get; } = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

		public override bool CanConvert(Type objectType) => objectType == typeof(long) || objectType == typeof(int);

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) => BaseTimestamp.AddMilliseconds((long)reader.Value).ToLocalTime();

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => writer.WriteValue((long)((((DateTime)value).ToUniversalTime() - BaseTimestamp).TotalMilliseconds + 0.5));
	}
}
