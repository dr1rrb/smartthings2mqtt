using System;
using System.Linq;

namespace SmartThings2MQTT.Smartthings.Model
{
	public partial class EventArgs
	{
		public string Name { get; set; }

		public string Value { get; set; }

		public string Date { get; set; }
	}
}