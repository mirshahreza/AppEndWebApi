using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AppEndWebApiHelper
{
	public record AppEndWebApiConfig
	{
		public bool CheckAccess { get; set; } = true;
		public List<string>? AllowedRoles { get; set; }
		public List<string>? AllowedUsers { get; set; }
		public List<string>? DeniedRoles { get; set; }
		public List<string>? DeniedUsers { get; set; }

		public CacheLevel CacheLevel { get; set; } = CacheLevel.None;

		public LogLevel LogLevel { get; set; } = LogLevel.None;
	}

	public static class AppEndWebApiConfigExtensions
	{
		public static void WriteConfig(this AppEndWebApiInfo appEndWebApiInfo,AppEndWebApiConfig config)
		{
			string configFileName = appEndWebApiInfo.GetConfigFileName();
			if (!Directory.Exists(configFileName))
			{
				Directory.CreateDirectory(configFileName);
			}
			var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
			File.WriteAllText(configFileName, json, Encoding.UTF8);
		}
		public static AppEndWebApiConfig ReadConfig(AppEndWebApiInfo appEndWebApiInfo)
		{
			string configFileName = appEndWebApiInfo.GetConfigFileName();
			if (File.Exists(configFileName))
			{
				var json = File.ReadAllText(configFileName, Encoding.UTF8);
				return JsonSerializer.Deserialize<AppEndWebApiConfig>(json) ?? new AppEndWebApiConfig();
			}
			return new AppEndWebApiConfig();
		}

		public static string GetConfigFileName(this AppEndWebApiInfo appEndWebApiInfo)
		{
			return $"workspace/server/{appEndWebApiInfo.ControllerName}.json";
		}
	}

	public enum CacheLevel
	{
		None = 0,
		AllUsers = 1,
		PerUser = 2
	}

	public enum LogLevel
	{
		None = 0,
		EventsOnly = 1,
		EventsPlusInputs = 2
	}

}