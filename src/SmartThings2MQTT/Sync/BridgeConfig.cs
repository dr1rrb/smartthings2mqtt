using System;
using System.Collections.Generic;
using System.Linq;
using SmartThings2MQTT.Utils;

namespace SmartThings2MQTT.Sync
{
	public sealed class BridgeConfig : IConfig
	{
		public string TopicNamespace { get; set; } = "smartthings";

		public string BridgeToStAuthToken { get; set; }

		public string StToBridgeAuthToken { get; set; }

		public IEnumerable<string> Validate()
		{
			if (TopicNamespace.IsEmpty())
			{
				yield return "TopicNamespace is invalid.";
			}
			if (BridgeToStAuthToken.IsEmpty())
			{
				yield return "BridgeToStAuthToken not configured.";
			}
			if (StToBridgeAuthToken.IsEmpty())
			{
				yield return "StToBridgeAuthToken not configured.";
			}
		}
	}
}