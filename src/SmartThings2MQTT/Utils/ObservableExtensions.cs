using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace SmartThings2MQTT.Utils
{
	internal static class ObservableExtensions
	{
		public static IObservable<T> Retry<T>(this IObservable<T> source, TimeSpan retryDelay, IScheduler scheduler)
			=> source.Catch(source.DelaySubscription(retryDelay, scheduler).Retry());
	}
}