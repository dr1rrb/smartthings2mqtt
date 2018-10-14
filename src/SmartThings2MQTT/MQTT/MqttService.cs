using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SmartThings2MQTT.MQTT.Model;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace SmartThings2MQTT.MQTT
{
	/// <summary>
	/// Basic service to manage connection to a MQTT broker
	/// </summary>
	public sealed class MqttService
	{
		private readonly MqttClient _client;
		private readonly MqttBrokerConfig _broker;
		private readonly string _stateTopic;
		private readonly IScheduler _messageLoopScheduler;
		private readonly IObservable<MqttMessage> _messages;

		private readonly MqttCache _cache = new MqttCache();

		private ImmutableHashSet<string> _subscribedTopics = ImmutableHashSet<string>.Empty;

		public MqttService(MqttBrokerConfig broker, string stateTopic, IScheduler messageLoopScheduler)
		{
			_client = new MqttClient(broker.Host, broker.Port, false, null, null, MqttSslProtocols.None);
			_broker = broker;
			_stateTopic = stateTopic;
			_messageLoopScheduler = messageLoopScheduler;

			_messages = Observable
				.FromEventPattern<MqttClient.MqttMsgPublishEventHandler, MqttMsgPublishEventArgs>(
					h => _client.MqttMsgPublishReceived += h,
					h => _client.MqttMsgPublishReceived -= h,
					_messageLoopScheduler)
				.Finally(() =>
				{
					_subscribedTopics = ImmutableHashSet<string>.Empty;
					_cache.Clear();
				})
				.ObserveOn(_messageLoopScheduler)
				.Select(evt => new MqttMessage(evt.EventArgs))
				.Where(message => _cache.Update(message.Topic, message.Value))
				.Publish()
				.RefCount();
		}

		/// <summary>
		/// Asynchronously publishes a message
		/// </summary>
		/// <param name="ct">Cancellation token to abort operation</param>
		/// <param name="topic">Topic of the message to publish</param>
		/// <param name="value">Data of the message to publish</param>
		/// <param name="qos">The qulity of service associted to the message</param>
		/// <param name="retain">A boolean which indicates if the broker should retain the message of not (cf. remark)</param>
		/// <remarks>
		/// A retained message is a normal MQTT message with the <paramref name="retain"/> flag set to true.
		/// The broker stores the last retained message and the corresponding QoS for that topic.
		/// Each client that subscribes to a topic pattern that matches the topic of the retained message receives the retained
		/// message immediately after they subscribe. The broker stores only one retained message per topic.
		/// </remarks>
		/// <returns></returns>
		public Task Publish(CancellationToken ct, string topic, string value, QualityOfService qos = QualityOfService.AtLeastOnce, bool retain = true)
			=> Publish(ct, new[] {(topic, value)}, qos, retain);

		/// <summary>
		/// Asynchronously publishes some messages
		/// </summary>
		/// <param name="ct">Cancellation token to abort operation</param>
		/// <param name="messages">Messages to publish</param>
		/// <param name="qos">The qulity of service associted to the message</param>
		/// <param name="retain">A boolean which indicates if the broker should retain the message of not (cf. remark)</param>
		/// <remarks>
		/// A retained message is a normal MQTT message with the <paramref name="retain"/> flag set to true.
		/// The broker stores the last retained message and the corresponding QoS for that topic.
		/// Each client that subscribes to a topic pattern that matches the topic of the retained message receives the retained
		/// message immediately after they subscribe. The broker stores only one retained message per topic.
		/// </remarks>
		/// <returns></returns>
		public Task<(string topic, string value)[]> Publish(CancellationToken ct, (string topic, string value)[] messages, QualityOfService qos = QualityOfService.AtLeastOnce, bool retain = true)
			=> _messageLoopScheduler.Run(ct, () =>
			{
				EnsureConnected();

				return Send().ToArray();

				IEnumerable<(string topic, string value)> Send()
				{
					foreach (var message in messages)
					{
						if (_cache.Update(message.topic, message.value))
						{
							_client.Publish(message.topic, Encoding.UTF8.GetBytes(message.value), (byte) qos, retain);
							yield return message;
						}
					}
				}
			});

		/// <summary>
		/// Gets an observable sequence of MQTT messages for all topics of a given prefix
		/// </summary>
		/// <param name="topicPrefix">The prefix to listen for (withou any # !)</param>
		/// <param name="qos">The quality of service to use to subscribe to topics</param>
		/// <returns></returns>
		public IObservable<MqttMessage> ObserveAll(string topicPrefix, QualityOfService qos = QualityOfService.AtLeastOnce)
		{
			// There is big bug here : If we subscribe first to 'rainbow/#' then 'unicorn/#' then we will have not receive
			// the retain message for 'unicorns' so we will have to wait for the next messages ...
			// Actually the observable is not even published :/

			return Observable
				.Create<MqttMessage>(async (observer, ct) =>
				{
					var subscription = _messages.Subscribe(observer);
					await Subscribe(ct, QualityOfService.AtLeastOnce, "#");
					return subscription;
				})
				.Where(message => message.Topic.StartsWith(topicPrefix, StringComparison.InvariantCultureIgnoreCase))
				.Do(message => TrySubscribe(qos, message.Topic));
		}

		private void EnsureConnected()
		{
			if (!_client.IsConnected)
			{
				_client.Connect(
					_broker.ClientId, 
					_broker.Username, 
					_broker.Password,
					willRetain: true,
					willQosLevel: (byte)QualityOfService.AtLeastOnce,
					willFlag: true,
					willTopic: _stateTopic,
					willMessage: "offline",
					cleanSession: false,
					keepAlivePeriod: 10);
				// Birth
				_client.Publish(_stateTopic, Encoding.UTF8.GetBytes("online"), (byte) QualityOfService.AtLeastOnce, retain: true);
			}
		}

		private ushort? TrySubscribe(QualityOfService qos, params string[] topics)
		{
			EnsureConnected();

			var updatedTopics = _subscribedTopics.ToBuilder();
			if (topics.Aggregate(false, (added, topic) => added | updatedTopics.Add(topic)))
			{
				var id = _client.Subscribe(topics, Enumerable.Repeat((byte)qos, topics.Length).ToArray());
				_subscribedTopics = updatedTopics.ToImmutable();

				return id;
			}
			else
			{
				return null;
			}
		}	

		private async Task Subscribe(CancellationToken ct, QualityOfService qos, params string[] topics)
		{
			var subscritions = Observable
				.FromEventPattern<MqttClient.MqttMsgSubscribedEventHandler, MqttMsgSubscribedEventArgs>(
					h => _client.MqttMsgSubscribed += h,
					h => _client.MqttMsgSubscribed -= h)
				.Replay(Scheduler.Immediate);

			var tcs = new TaskCompletionSource<Unit>();
			using (ct.Register(() => tcs.TrySetCanceled()))
			using (subscritions.Connect())
			{
				var id = await _messageLoopScheduler.Run(ct, () => TrySubscribe(qos, topics));
				if (id == null)
				{
					// Already subscribed
					return;
				}

				await subscritions
					.FirstAsync(args => args.EventArgs.MessageId == id)
					.Timeout(TimeSpan.FromSeconds(30), _messageLoopScheduler)
					.ToTask(ct);
			}
		}
	}
}