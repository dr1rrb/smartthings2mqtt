using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;

namespace SmartThings2MQTT.MQTT
{
	internal static class SchedulerExtensions
	{
		public static Task Run(this IScheduler scheduler, CancellationToken ct, Action action) 
			=> scheduler.Run(ct, () =>
			{
				action();
				return Unit.Default;
			});

		public static async Task<T> Run<T>(this IScheduler scheduler, CancellationToken ct, Func<T> action)
		{
			var tcs = new TaskCompletionSource<T>();
			if (ct.CanBeCanceled)
			{
				ct.Register(() => tcs.TrySetCanceled());
			}

			scheduler.Schedule(() =>
			{
				try
				{
					tcs.TrySetResult(action());
				}
				catch (Exception e)
				{
					tcs.TrySetException(e);
				}
			});

			return await tcs.Task;
		}
	}
}