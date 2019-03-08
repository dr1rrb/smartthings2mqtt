using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SmartThings2MQTT.Logging;
using SmartThings2MQTT.Smartthings.Model;
using SmartThings2MQTT.Sync;

namespace SmartThings2MQTT.Smartthings.Controllers
{
	[Route("api/[controller]")]
	public class SmartthingsController : Controller
	{
		private readonly Synchronizer _mqtt;
		private readonly string _authHeader;

		public SmartthingsController(Synchronizer mqtt, BridgeConfig config)
		{
			_mqtt = mqtt;
			_authHeader = "Bearer " + config.StToBridgeAuthToken;
		}

		[HttpGet]
		public string Get() => "Running!";

		[HttpPost]
		public async Task Post([FromBody]SmartthingsToMqttRequestData request, CancellationToken ct)
		{
			if (/*!Request.IsHttps
				||*/ !Request.Headers.TryGetValue("Authorization", out var auth)
				|| !auth.Equals(_authHeader))
			{
				this.Log().Warning("Unauthorized");

				return;
			}

			switch (request.Kind)
			{
				case SmartthingsToMqttRequestKind.Device:
					await _mqtt.Update(ct, request.Device);
					break;

				case SmartthingsToMqttRequestKind.Routine:
				await _mqtt.Execute(ct, request.Routine, request.Event.Date);
					break;

				default:
					throw new ArgumentOutOfRangeException( nameof(request.Kind), "Unknown kind: " + request.Kind);
			}

			
		}
	}
}
