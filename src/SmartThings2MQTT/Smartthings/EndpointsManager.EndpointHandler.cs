using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SmartThings2MQTT.Logging;
using SmartThings2MQTT.Smartthings.Model;

namespace SmartThings2MQTT.Smartthings
{
	partial class EndpointsManager
	{
		private class EndpointHandler : IAppEndpoint
		{
			private readonly Endpoint _endpoint;
			private readonly HttpClient _client;

			public EndpointHandler(Endpoint endpoint, HttpClient client)
			{
				_endpoint = endpoint;
				_client = client;

				CreationDate = DateTimeOffset.Now;
			}

			public DateTimeOffset CreationDate { get; }

			public async Task<Device[]> GetDevices(CancellationToken ct, bool detailed = false)
			{
				var response = await _client.GetAsync(new Uri(Path.Combine(_endpoint.Uri, "items") + "?details=" + detailed.ToString().ToLowerInvariant()), ct);
				var content = await response.Content.ReadAsStringAsync();
				var devices = JsonConvert.DeserializeObject<DevicesResponseData>(content).Devices;

				return devices;
			}

			public async Task Execute(CancellationToken ct, string deviceId, string command, IDictionary<string, object> parameters)
			{
				try
				{
					(await _client
						.PutAsync(
							new Uri(Path.Combine(_endpoint.Uri, "device", deviceId, command)), 
							new StringContent(JsonConvert.SerializeObject(parameters), Encoding.UTF8, "application/json"), ct))
						.EnsureSuccessStatusCode();
				}
				catch (Exception e)
				{
					this.Log().Error("Failed to forward notification to smartthinhgs", e);
				}
			}
		}
	}
}