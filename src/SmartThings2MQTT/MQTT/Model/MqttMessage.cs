using System;
using System.Linq;
using System.Text;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace SmartThings2MQTT.MQTT.Model
{
	public sealed class MqttMessage
	{
		private readonly MqttMsgPublishEventArgs _args;

		public MqttMessage(MqttMsgPublishEventArgs args)
		{
			_args = args;
		}

		public string Topic => _args.Topic;

		public string Value => Encoding.UTF8.GetString(_args.Message);

		public override string ToString() => $"{Topic} = {Value}";
	}
}