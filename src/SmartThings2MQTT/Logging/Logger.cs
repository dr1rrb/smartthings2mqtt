using System;
using System.Linq;

namespace SmartThings2MQTT.Logging
{
	public static class Logger
	{
		public static ILogger Log(this object owner)
		{
			return SerilogAdapter.Instance;
		}
	}
}