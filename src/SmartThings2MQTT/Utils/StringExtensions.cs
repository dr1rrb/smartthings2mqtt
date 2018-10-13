using System;
using System.Linq;

namespace SmartThings2MQTT.Utils
{
	internal static class StringExtensions
	{
		public static bool IsEmpty(this string value)
			=> string.IsNullOrWhiteSpace(value);

		public static bool HasValue(this string value)
			=> !string.IsNullOrWhiteSpace(value);
	}
}