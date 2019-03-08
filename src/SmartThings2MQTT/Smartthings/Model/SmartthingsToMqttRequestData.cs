using System;
using System.Linq;

namespace SmartThings2MQTT.Smartthings.Model
{
	public partial class SmartthingsToMqttRequestData
	{
		public SmartthingsToMqttRequestKind Kind { get; set; }

		public Device Device { get; set; }

		public Routine Routine { get; set; }

		public EventArgs Event { get; set; }
	}
}