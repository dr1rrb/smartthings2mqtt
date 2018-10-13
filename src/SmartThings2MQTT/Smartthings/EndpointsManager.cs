using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SmartThings2MQTT.Smartthings.Model;
using SmartThings2MQTT.Utils;
using AsyncLock = SmartThings2MQTT.Utils.AsyncLock;

namespace SmartThings2MQTT.Smartthings
{
	public sealed partial class EndpointsManager : IDisposable
	{
		private const int _maxErrorCount = 5;

		private readonly AsyncLock _endpointsGate = new AsyncLock();
		public readonly CancellationDisposable _token = new CancellationDisposable();
		private readonly HttpClient _client;
		private readonly IScheduler _scheduler;

		private ImmutableDictionary<string, IAppEndpoint> _endpoints = ImmutableDictionary<string, IAppEndpoint>.Empty.WithComparers(StringComparer.OrdinalIgnoreCase);
		private int _errorCount = 0;

		public EndpointsManager(string authToken, IScheduler scheduler)
		{
			_scheduler = scheduler;
			_client = new HttpClient
			{
				DefaultRequestHeaders =
				{
					{"Authorization", $"Bearer {authToken}"}
				}
			};
		}

		public IObservable<IAppEndpoint> GetAndObserveEndpoint(string locationId)
		{
			return Observable
				.StartAsync(async ct => await GetEndpoint(ct, locationId), _scheduler)
				.Retry(Constants.InvalidEndpointDuration, _scheduler);
		}

		public async Task<IAppEndpoint> GetEndpoint(CancellationToken ct, string locationId)
		{
			var endpoint = await FindEndpoint(ct, locationId);
			if (endpoint is NullEndpoint)
			{
				throw new InvalidOperationException("Cannot find endpoint");
			}

			return endpoint;
		}

		public async Task<IAppEndpoint> FindEndpoint(CancellationToken ct, string locationId)
		{
			var endpoints = _endpoints;
			if (endpoints == null)
			{
				// endpoints was erased, we are in an invalid state (too much exceptions)
				return null;
			}

			if (endpoints.TryGetValue(locationId, out var endpoint)
				&& IsValid(endpoint))
			{
				return endpoint;
			}

			ct = _token.Token; // Do not abort a load operation (and keep the lock locked)

			using (await _endpointsGate.LockAsync(ct))
			{
				if (endpoints.TryGetValue(locationId, out endpoint)
					&& IsValid(endpoint))
				{
					return endpoint;
				}

				endpoints = _endpoints = await LoadEndpoints(ct);

				if (endpoints.TryGetValue(locationId, out endpoint)) // No needs to check if 'endpoint' is valid here
				{
					return endpoint;
				}
				else
				{
					endpoint = new NullEndpoint(locationId, _scheduler);
					_endpoints = endpoints.Add(locationId, endpoint);

					return endpoint;
				}
			}
		}

		private bool IsValid(IAppEndpoint channel) 
			=> !(channel is NullEndpoint nullEndpoint) || !nullEndpoint.ShouldBeRevalidated();

		private async Task<ImmutableDictionary<string, IAppEndpoint>> LoadEndpoints(CancellationToken ct)
		{
			var uri = "https://graph.api.smartthings.com/api/smartapps/endpoints";
			var response = await _client.GetAsync(new Uri(uri), ct);

			if (response.StatusCode == HttpStatusCode.Unauthorized)
			{
				return null;
			}
			else if (!response.IsSuccessStatusCode && Interlocked.Increment(ref _errorCount) >= _maxErrorCount)
			{
				return null;
			}

			var data = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();
			var endpoints = JsonConvert.DeserializeObject<Endpoint[]>(data);

			if (!endpoints.Any())
			{
				return null;
			}

			_errorCount = 0;
			return endpoints.ToImmutableDictionary(e => e.Location.Id, e => new EndpointHandler(e, _client) as IAppEndpoint, StringComparer.OrdinalIgnoreCase);
		}

		public void Dispose() 
			=> _token.Dispose();
	}
}