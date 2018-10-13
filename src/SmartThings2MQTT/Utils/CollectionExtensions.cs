using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartThings2MQTT.Utils
{
	internal static class CollectionExtensions
	{
		public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
		{
			foreach (var item in items)
			{
				collection.Add(item);
			}
		}
	}
}