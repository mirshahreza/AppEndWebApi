using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Filters;
using Serilog.Sinks.MSSqlServer;
using Serilog.Context;
using System.Collections.ObjectModel;
using System.Data;
using Newtonsoft.Json.Linq;
using System.Reflection;
using AppEndCommon;

namespace AppEndWebApiHelper
{
	public static class AppEndLogger
	{
		public static void SetupLoggers()
		{
			var loggerConf = new LoggerConfiguration().MinimumLevel.Verbose();

			loggerConf.WriteTo.Logger(lc => lc
				.Filter.ByIncludingOnly(e => (e.Level== Serilog.Events.LogEventLevel.Information))
				.WriteTo.Console(
					outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message}{NewLine}{Exception}"
				));

			loggerConf.WriteTo.Logger(lc => lc
				.Filter.ByIncludingOnly(e => (e.Level == Serilog.Events.LogEventLevel.Error))
				.WriteTo.File(
					path: "workspace/log/error-.txt",
					rollingInterval: RollingInterval.Hour,
					outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message}{NewLine}{Exception}",
					fileSizeLimitBytes: 10 * 1024 * 1024,
					retainedFileCountLimit: 30
				));

			loggerConf.WriteTo.Logger(lc => lc
				.Filter.ByIncludingOnly(e => (e.Level == Serilog.Events.LogEventLevel.Debug))
				.WriteTo.File(
					path: "workspace/log/debug-.txt",
					rollingInterval: RollingInterval.Hour,
					outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message}{NewLine}{Exception}",
					fileSizeLimitBytes: 10 * 1024 * 1024,
					retainedFileCountLimit: 30
				));

			loggerConf.WriteTo.Logger(lc => lc
				.Filter.ByIncludingOnly(e => (e.Level == Serilog.Events.LogEventLevel.Warning))
				.WriteTo.File(
					path: "workspace/log/warning-.txt",
					rollingInterval: RollingInterval.Hour,
					outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message}{NewLine}{Exception}",
					fileSizeLimitBytes: 10 * 1024 * 1024,
					retainedFileCountLimit: 30
				));

			loggerConf.WriteTo.Logger(lc => lc
				.Filter.ByIncludingOnly(e => (e.Level == Serilog.Events.LogEventLevel.Verbose))
				.WriteTo.MSSqlServer(
					connectionString: GetSerilogConnectionString(),
					sinkOptions: new MSSqlServerSinkOptions { 
						TableName = "AppEnd_ActivityLog", 
						BatchPostingLimit = GetBatchPostingLimit(),
						BatchPeriod = GetBatchPeriodSeconds()
					},
					columnOptions: GetColumnOptions()
				));

			Log.Logger = loggerConf.CreateLogger();
		}

		


		public static void LogConsole(string message)
		{
			Log.Information(message);
		}

		public static void LogDebug(string message)
		{
			Log.Debug(message);
		}
		public static void LogError(string message)
		{
			Log.Error(message);
		}
		public static void LogWarning(string message)
		{
			Log.Warning(message);
		}

		public static void LogActivity(string controller, string method, string commadId, bool result, string message, int duration, string clientAgent, string clientIp, string actor)
		{
			Log.Logger.Verbose("{Controller}{Method}{RowId}{Result}{Message}{Duration}{ClientAgent}{ClientIp}{EventBy}{EventOn}",
				controller, method, commadId, result, message, duration, clientAgent, clientIp, actor, DateTime.Now);
		}

		private static string GetSerilogConnectionString()
		{
			return ProjectHelpers.GetConnectionStringByName(ProjectHelpers.AppEndSection["Serilog"]?["Connection"]?.ToString() ?? "DefaultConnection");
		}

		private static int GetBatchPostingLimit()
		{
			return (ProjectHelpers.AppEndSection["Serilog"]?["BatchPostingLimit"]?.ToString() ?? "100").ToIntSafe();
		}
		private static TimeSpan GetBatchPeriodSeconds()
		{
			return new TimeSpan(0, 0, (ProjectHelpers.AppEndSection["Serilog"]?["BatchPeriodSeconds"]?.ToString() ?? "15").ToIntSafe());
		}

		private static ColumnOptions GetColumnOptions()
		{
			var columnOptions = new ColumnOptions();

			columnOptions.Store.Remove(StandardColumn.MessageTemplate);
			columnOptions.Store.Remove(StandardColumn.Message);
			columnOptions.Store.Remove(StandardColumn.Exception);
			columnOptions.Store.Remove(StandardColumn.Level);
			columnOptions.Store.Remove(StandardColumn.LogEvent);
			columnOptions.Store.Remove(StandardColumn.Properties);
			columnOptions.Store.Remove(StandardColumn.TimeStamp);

			columnOptions.Id.ColumnName = "Id";

			columnOptions.AdditionalColumns =
			[
				new SqlColumn() { ColumnName = "Controller", DataType = SqlDbType.VarChar, DataLength = 64 },
				new SqlColumn() { ColumnName = "Method", DataType = SqlDbType.VarChar, DataLength = 64 },
				new SqlColumn() { ColumnName = "RowId", DataType = SqlDbType.VarChar, DataLength = 64 },
				new SqlColumn() { ColumnName = "Result", DataType = SqlDbType.Bit },
				new SqlColumn() { ColumnName = "Message", DataType = SqlDbType.VarChar, DataLength = 128 },
				new SqlColumn() { ColumnName = "Duration", DataType = SqlDbType.Int },
				new SqlColumn() { ColumnName = "ClientAgent", DataType = SqlDbType.NVarChar, DataLength = 256 },
				new SqlColumn() { ColumnName = "ClientIp", DataType = SqlDbType.VarChar, DataLength = 32 },
				new SqlColumn() { ColumnName = "EventBy", DataType = SqlDbType.NVarChar, DataLength = 64 },
				new SqlColumn() { ColumnName = "EventOn", DataType = SqlDbType.DateTime },
			];

			return columnOptions;
		}

	}
}
