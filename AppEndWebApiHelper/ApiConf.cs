using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AppEndWebApiHelper
{
	public record ApiConf
	{
		public CheckAccessLevel CheckAccessLevel { get; set; } = CheckAccessLevel.CheckAccessRules;

		public List<string>? AllowedRoles { get; set; }
		public List<string>? AllowedUsers { get; set; }
		public List<string>? DeniedRoles { get; set; }
		public List<string>? DeniedUsers { get; set; }

		public CacheLevel CacheLevel { get; set; } = CacheLevel.None;

		public bool LogEnabled { get; set; } = true;
	}

	public static class AppEndWebApiConfigExtensions
	{
		public static void WriteConfig(this ApiInfo appEndWebApiInfo,ApiConf config)
		{
			string configFileName = appEndWebApiInfo.GetConfigFileName();
			if (!Directory.Exists(configFileName))
			{
				Directory.CreateDirectory(configFileName);
			}
			var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
			File.WriteAllText(configFileName, json, Encoding.UTF8);
		}
		public static ApiConf ReadConfig(this ApiInfo appEndWebApiInfo)
		{
			string configFileName = appEndWebApiInfo.GetConfigFileName();
			if (File.Exists(configFileName))
			{
				var json = File.ReadAllText(configFileName, Encoding.UTF8);
				return JsonSerializer.Deserialize<ApiConf>(json) ?? new ApiConf();
			}
			return new ApiConf();
		}

		public static string GetConfigFileName(this ApiInfo appEndWebApiInfo)
		{
			return $"workspace/server/{appEndWebApiInfo.ControllerName}.config.json";
		}
	}

	public enum CacheLevel
	{
		None = 0,
		AllUsers = 1,
		PerUser = 2
	}

	public enum CheckAccessLevel
	{
		CheckAccessRules = 0,
		OpenForAuthenticatedUsers = 1,
		OpenForAllUsers = 2
	}


}