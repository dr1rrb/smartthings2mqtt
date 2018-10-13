using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmartThings2MQTT.Smartthings.Model;

namespace SmartThings2MQTT.Smartthings
{
	public interface IAppEndpoint
	{
		Task Execute(CancellationToken ct, string deviceId, string command, IDictionary<string, object> parameters);

		Task<Device[]> GetDevices(CancellationToken ct, bool detailed = false);
	}
}