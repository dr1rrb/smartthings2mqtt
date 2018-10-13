using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartThings2MQTT.Utils
{
	public interface IConfig
	{
		IEnumerable<string> Validate();
	}
}