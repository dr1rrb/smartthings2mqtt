using System;
using System.Collections.Immutable;
using System.Linq;

namespace SmartThings2MQTT.Smartthings.Model
{
	public partial class Device
	{
		public string Id { get; set; }

		public string Name { get; set; }

		public IImmutableDictionary<string, string> Properties { get; set; }
	}
}