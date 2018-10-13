using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartThings2MQTT.MQTT
{
	internal class MqttCache
	{
		private readonly Dictionary<string, string> _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		public bool Update(string topic, string value)
		{
			if (_values.TryGetValue(topic, out var currentValue)
				&& currentValue.Equals(value, StringComparison.InvariantCultureIgnoreCase))
			{
				return false;
			}
			else
			{
				_values[topic] = value;

				return true;
			}
		}

		public void Clear() => _values.Clear();
	}
}