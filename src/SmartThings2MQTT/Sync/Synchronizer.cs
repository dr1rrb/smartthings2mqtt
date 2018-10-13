using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmartThings2MQTT.Logging;
using SmartThings2MQTT.MQTT;
using SmartThings2MQTT.MQTT.Model;
using SmartThings2MQTT.Smartthings;
using SmartThings2MQTT.Smartthings.Model;
using SmartThings2MQTT.Utils;
using AsyncLock = SmartThings2MQTT.Utils.AsyncLock;

namespace SmartThings2MQTT.Sync
{
	/// <summary>
	/// Ensure the synchronization between Smatthings and the MQTT broker
	/// </summary>
	public sealed class Synchronizer : IDisposable
	{
		private readonly SingleAssignmentDisposable _subscription = new SingleAssignmentDisposable();
		private readonly AsyncLock _gate = new AsyncLock();

		private readonly SmartthingsConfig _smartthingsConfig;
		private readonly MqttService _mqtt;
		private readonly BridgeConfig _mqttConfig;
		private readonly EndpointsManager _smartthings;
		private readonly IScheduler _scheduler;

		private int _isEnabled = 0;

		public Synchronizer(MqttService mqtt, BridgeConfig mqttConfig, EndpointsManager smartthings, SmartthingsConfig stConfig, IScheduler scheduler)
		{
			_mqtt = mqtt;
			_mqttConfig = mqttConfig;
			_smartthings = smartthings;
			_smartthingsConfig = stConfig;
			_scheduler = scheduler;
		}

		/// <summary>
		/// Start the synchronizer
		/// </summary>
		public void Enable()
		{
			if (Interlocked.CompareExchange(ref _isEnabled, 1, 0) == 0)
			{
				var smartthings = default((IAppEndpoint endpoint, Device[] devices));

				var stToMqtt = GetAndObserveSmartthings()
					.Do(st => smartthings = st)
					.Select(st => Observable.FromAsync(ct => SmartthingToMqtt(ct, st.devices), _scheduler))
					.Switch()
					.Retry(TimeSpan.FromMinutes(1), _scheduler)
					.Publish();

				var mqttToSt = _mqtt
					.ObserveAll(_mqttConfig.TopicNamespace)
					// We skip the first batch of items until the current state from ST init the cache
					.SkipUntil(stToMqtt.FirstAsync())
					.SelectMany(async (message, ct) => MqttToSmartthings(ct, message, smartthings.devices, smartthings.endpoint))
					.Retry(TimeSpan.FromMinutes(1), _scheduler);

				_subscription.Disposable = new CompositeDisposable(
					mqttToSt.Subscribe(),
					stToMqtt.Connect());
			}
		}

		/// <summary>
		/// Updates MQTT broker from a ST device
		/// </summary>
		public Task Update(CancellationToken ct, Device device) => SmartthingToMqtt(ct, device);

		private async Task SmartthingToMqtt(CancellationToken ct, params Device[] devices)
		{
			var messages = devices
				.SelectMany(device => device
					.Properties
					.Where(property => property.Value?.HasValue() ?? false)
					.Select(property =>
					{
						var topic = $"{_mqttConfig.TopicNamespace}/{device.Id.ToLowerInvariant()}/{property.Key.ToLowerInvariant()}";
						var hasValue = property.Value?.HasValue() ?? false;
						var value = hasValue
							? property.Value.ToLowerInvariant()
							: null;

						return (device: device, topic: topic, hasValue: hasValue, value: value);
					}))
				.Where(message => message.hasValue)
				.ToArray();

			if (messages.Any())
			{
				foreach (var message in messages)
				{
					this.Log().Info($"[ST => MQTT] Sending ({message.device.Name}): {message.topic} = {message.value}");
				}

				var sent = await _mqtt.Publish(ct, messages.Select(message => (topic: message.topic, value: message.value)).ToArray());

				foreach (var message in sent)
				{
					this.Log().Info($"[ST => MQTT] Sent: {message.topic} = {message.value}");
				}
			}
		}

		private async Task<Unit> MqttToSmartthings(CancellationToken ct, MqttMessage message, Device[] devices, IAppEndpoint endpoint)
		{
			try
			{
				var topicParts = message.Topic.Split(new[] { '/' }, 3, StringSplitOptions.RemoveEmptyEntries);
				//var topicNamespace = topicParts[0];
				var topicDeviceId = topicParts[1];
				var topicProperty = topicParts[2];

				var device = devices.SingleOrDefault(d => d.Id.Equals(topicDeviceId, StringComparison.InvariantCultureIgnoreCase));
				if (device == null)
				{
					this.Log().Warning("No device found on ST for id: " + topicDeviceId);
					return Unit.Default;
				}

				if (!TryGetCommand(device, topicProperty, message.Value, out var command, out var parameters))
				{
					this.Log().Warning($"No command found on {device.Name} for {message}");
					return Unit.Default;
				}

				using (await _gate.LockAsync(ct))
				{
					this.Log().Info($"[MQTT => ST] Sending ({device.Name}): {message}");

					await endpoint.Execute(ct, topicDeviceId, command, parameters);

					this.Log().Info($"[MQTT => ST] Sent {message} ({device.Name}.{command}({parameters.FirstOrDefault().Value ?? ""}))");
				}
			}
			catch (Exception e)
			{
				this.Log().Error($"[MQTT => ST] Failed to send: {message}", e);
			}

			return Unit.Default;
		}

		private IObservable<(IAppEndpoint endpoint, Device[] devices)> GetAndObserveSmartthings()
		{
			return _smartthings
				.GetAndObserveEndpoint(_smartthingsConfig.LocationId)
				.Select(ep => Observable
					.Timer(TimeSpan.Zero, TimeSpan.FromMinutes(10), _scheduler)
					.Select(_ => Observable.StartAsync(async ct => (endpoint: ep, devices: await ep.GetDevices(ct, detailed: true))))
					.Switch()
				)
				.Switch()
				.Retry(TimeSpan.FromMinutes(1), _scheduler)
				.DistinctUntilChanged(d => d.devices);
		}

		private bool TryGetCommand(Device device, string property, string value, out string command, out IDictionary<string, object> parameters)
		{
			command = null;
			parameters = new Dictionary<string, object>();
			value = value.ToLowerInvariant();

			switch (property.ToLowerInvariant())
			{
				case "switch":
					command = value.ToLowerInvariant();
					break;

				case "level":
					command = "setLevel";
					parameters = new Dictionary<string, object> {{"level", int.Parse(value)}};
					break;

				case "nightmode" when value == "enabled":
					command = "enableNightmode";
					break;

				case "nightmode":
					command = "disableNightmode";
					break;

			}

			return command != null;
		}

		private bool TryParse(Device device, string property, string value, out object parsedValue)
		{
			parsedValue = null;
			switch (property.ToLowerInvariant())
			{
				case "switch":
					parsedValue = value.ToLowerInvariant();
					break;

				case "level":
					parsedValue = int.Parse(value);
					break;
			}

			return parsedValue != null;
		}

		/// <inheritdoc />
		public void Dispose() => _subscription.Dispose();
	}
}