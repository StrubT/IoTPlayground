using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace StrubT.IoT.Playground {

	class SiotCenter {

		[JsonProperty("licence")]
		public string Guid { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("url")]
		public string Url { get; set; }

		[JsonProperty("port")]
		public string Port { get; set; }

		[JsonProperty("webSocketPort")]
		public string WebSocketPort { get; set; }
	}

	class SiotSensorActorManifest {

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("type")]
		public string Type { get; set; }

		[JsonProperty("zone")]
		public SiotZone Zone { get; set; }

		[JsonProperty("description")]
		public string Description { get; set; }

		[JsonProperty("valueType")]
		public string ValueType { get; set; }

		[JsonProperty("jsonMapping")]
		public IDictionary<string, string> JsonMapping { get; set; }

		//[JsonProperty("file")]
		//public SiotFileInformation FileInformation { get; set; } //new { name = string.Empty, type = string.Empty, size = 0L, date = 0L }
	}

	class SiotZone {

		[JsonProperty("guid")]
		public string Guid { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }
	}

	class SiotCenterConfiguration {

		[JsonProperty("storage")]
		public string Storage { get; set; }
	}

	class SiotHistoryValue<T> {

		[JsonProperty("data")]
		public T Data { get; set; }

		[JsonProperty("time"), JsonConverter(typeof(Json.SiotDateTimeConverter))]
		public DateTime DateTime { get; set; }
	}
}
