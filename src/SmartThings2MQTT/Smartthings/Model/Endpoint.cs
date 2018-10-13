using System;
using System.Linq;
using Newtonsoft.Json;

namespace SmartThings2MQTT.Smartthings.Model
{
	public class Endpoint
	{
		public Location Location { get; set; }

		[JsonProperty("uri")]
		public string Uri { get; set; }

		[JsonProperty("base_url")]
		public string Base { get; set; }

		[JsonProperty("url")]
		public string InstallationPath { get; set; }
	}
}