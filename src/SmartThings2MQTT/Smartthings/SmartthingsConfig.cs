using System;
using System.Collections.Generic;
using System.Linq;
using SmartThings2MQTT.Utils;

namespace SmartThings2MQTT.Smartthings
{
	public sealed class SmartthingsConfig : IConfig
	{
		public string LocationId { get; set; }

		public IEnumerable<string> Validate()
		{
			if (LocationId.IsEmpty())
			{
				yield return "LocationId not configured.";
			}
		}
	}
}