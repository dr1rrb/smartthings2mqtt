using System;
using System.Linq;

namespace SmartThings2MQTT.Smartthings.Model
{
	public enum SmartthingsToMqttRequestKind
	{
		Device = 0, // Default for backward compat

		Routine
	}
}