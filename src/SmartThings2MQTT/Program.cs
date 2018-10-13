using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace SmartThings2MQTT
{
	public class Program
	{
		public static void Main(string[] args)
		{
			try
			{
				BuildWebHost(args).Run();
			}
			catch (Exception e)
			{
				Log.Fatal(e, "Host terminated unexpectedly");

				throw;
			}
			finally
			{
				Log.CloseAndFlush();
			}
		}

		public static IWebHost BuildWebHost(string[] args) => WebHost
			.CreateDefaultBuilder(args)
			.ConfigureAppConfiguration(config => config
#if DEBUG
				.SetBasePath(Directory.GetCurrentDirectory())
#else
				.SetBasePath("/smartthings2mqtt")
#endif
				.AddJsonFile("config.json", optional: false)
			)
			.UseSerilog((host, logger) => logger
				.MinimumLevel.Is(host.Configuration.GetValue<LogEventLevel>("LogLevel"))
				.WriteTo.Console()
#if !DEBUG
				.WriteTo.RollingFile("/smartthings2mqtt/logs/{Date}.log", LogEventLevel.Information)
#endif
			)
			.UseStartup<Startup>()
			.UseUrls("http://0.0.0.0:1983")
			.Build();
	}
}
