using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

		public static AppEndWebApiConfig DefaultConfig()
		{
			return new AppEndWebApiConfig();
		}

		public static AppEndWebApiConfig FromFile()
		{
			return new AppEndWebApiConfig();
		}

	}

	public static class AppEndWebApiConfigExtensions
	{
		public static void WriteConfig(this AppEndWebApiConfig config)
		{
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