using System;
using System.Collections.Generic;
using System.Linq;
using SmartThings2MQTT.Utils;

namespace SmartThings2MQTT.MQTT
{
	public sealed class MqttBrokerConfig : IConfig
	{
		public string Host { get; set; }

		public int Port { get; set; } = 1883;

		public string Username { get; set; }

		public string Password { get; set; }

		public string ClientId { get; set; } = Environment.MachineName + "_SmartThings2MQTT";

		public IEnumerable<string> Validate()
		{
			if (Host.IsEmpty())
			{
				yield return "Host not configured.";
			}
			if (Port <= 0)
			{
				yield return $"Port {Port} is invalid.";
			}
			if (ClientId.IsEmpty())
			{
				yield return "Host not configured.";
			}
		}
	}
}