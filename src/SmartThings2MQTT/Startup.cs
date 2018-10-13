using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using SmartThings2MQTT.MQTT;
using SmartThings2MQTT.Smartthings;
using SmartThings2MQTT.Sync;
using SmartThings2MQTT.Utils;

namespace SmartThings2MQTT
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddMvc();

			var scheduler = new EventLoopScheduler();

			services
				.AddConfiguration(Configuration, config => config
					.Add<MqttBrokerConfig>("Broker")
					.Add<SmartthingsConfig>("SmartThings")
					.Add<BridgeConfig>("Bridge"))
				.AddSingleton<MqttService>(svc => new MqttService(
					svc.GetService<MqttBrokerConfig>(),
					scheduler))
				.AddSingleton<EndpointsManager>(svc => new EndpointsManager(
					svc.GetService<BridgeConfig>().BridgeToStAuthToken, 
					scheduler))
				.AddSingleton<Synchronizer>(svc => new Synchronizer(
					svc.GetService<MqttService>(),
					svc.GetService<BridgeConfig>(),
					svc.GetService<EndpointsManager>(),
					svc.GetService<SmartthingsConfig>(),
					scheduler));
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseMvc();
			app.ApplicationServices.GetService<Synchronizer>().Enable();
		}
	}
}
