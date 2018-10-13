using System;
using System.Linq;

namespace SmartThings2MQTT.Smartthings.Model
{
	public partial class SmartthingsToMqttRequestData
	{
		public Device Device { get; set; }

		public EventArgs Event { get; set; }
	}
}