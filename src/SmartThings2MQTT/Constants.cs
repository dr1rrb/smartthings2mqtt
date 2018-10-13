using System;
using System.Linq;

namespace SmartThings2MQTT
{
	public static class Constants
	{
		/// <summary>
		/// The delay at which an invalid enpoint should be revalidated (usually a missing endpoint for a given location)
		/// </summary>
		public static TimeSpan InvalidEndpointDuration { get; } = TimeSpan.FromMinutes(10);
	}
}