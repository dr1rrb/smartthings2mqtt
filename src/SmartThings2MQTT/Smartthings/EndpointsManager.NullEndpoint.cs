using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using SmartThings2MQTT.Logging;
using SmartThings2MQTT.Smartthings.Model;

namespace SmartThings2MQTT.Smartthings
{
	partial class EndpointsManager
	{
		private class NullEndpoint : IAppEndpoint
		{
			private readonly string _locationId;
			private readonly IScheduler _timeProvider;
			private readonly DateTimeOffset _createdOn;

			public NullEndpoint(string locationId, IScheduler timeProvider)
			{
				_locationId = locationId;
				_timeProvider = timeProvider;

				_createdOn = timeProvider.Now;
			}

			public bool ShouldBeRevalidated() 
				=> _createdOn + Constants.InvalidEndpointDuration < _timeProvider.Now;

			public async Task Execute(CancellationToken ct, string deviceId, string command, IDictionary<string, object> parameters) 
				=> Logger.Log(this).Info("Cannot send notification since the channel is currently invalid.");

			/// <inheritdoc />
			public Task<Device[]> GetDevices(CancellationToken ct, bool detailed = false)
				=> throw new NotSupportedException();
		}
	}
}