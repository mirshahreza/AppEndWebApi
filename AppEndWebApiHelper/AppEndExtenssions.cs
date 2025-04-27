using AppEndCommon;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AppEndWebApiHelper
{
	public static class AppEndExtenssions
	{
		//public static Serilog.ILogger AppEndLogger
		//{
		//	get
		//	{
		//		if (Log.Logger == null) 
		//		{
		//			Log.Logger = new LoggerConfiguration()
		//			.WriteTo.Console()
		//			.WriteTo.File("log.txt",
		//				rollingInterval: RollingInterval.Day,
		//				rollOnFileSizeLimit: true)
		//			.CreateLogger();
		//		}
		//		return Log.Logger;
		//	}
		//}

		public static ClaimsPrincipal TurnTokenToUser(this HttpContext context, ILogger<AppEndMiddleware> _logger)
		{
			if (context.Request.Headers.ContainsKey("token") == false)
			{
				return GetNobodyUser();
			}
			else
			{
				try
				{
					string t = context.Request.Headers["token"].ToStringEmpty().Replace("bearer ", "").Decode(ProjectHelpers.EncriptionSecret);
					JsonElement jo = t.ToJsonElementByBuiltIn();
					var claims = new List<Claim>
					{
						new Claim("Id", "65423"),
						new Claim(ClaimTypes.Name, "TestUser"),
						new Claim(ClaimTypes.Role, "User"),
						new Claim(ClaimTypes.UserData, "CustomValue")
					};
					var identity = new ClaimsIdentity(claims, "AppEndAuth");
					return new ClaimsPrincipal(identity);
				}
				catch
				{
					Log.Warning($"Invalid token tried !!!");
					return GetNobodyUser();
				}
			}
		}

		public static ClaimsPrincipal GetNobodyUser()
		{
			var claims = new List<Claim>
					{
						new Claim("Id", "-1"),
						new Claim(ClaimTypes.Name, "Nobody")
					};
			var identity = new ClaimsIdentity(claims, "AppEndAuth");
			return new ClaimsPrincipal(identity);
		}

		public static AppEndWebApiInfo GetAppEndWebApiInfo(this HttpContext context)
		{
			var routeData = context.GetRouteData();
			return new AppEndWebApiInfo(context.Request.Path.ToString(), routeData.Values["controller"].ToStringEmpty(), routeData.Values["action"].ToStringEmpty());
		}

		public static void AddSuccessHeaders(this HttpContext context, Stopwatch sw, AppEndWebApiInfo appEndWebApiInfo)
		{
			AddAppEndStandardHeaders(context, sw, appEndWebApiInfo, StatusCodes.Status200OK, "Status200OK", "OK");
		}

		public static void AddInternalErrorHeaders(this HttpContext context, Stopwatch sw, Exception ex, AppEndWebApiInfo appEndWebApiInfo)
		{
			AddAppEndStandardHeaders(context, sw, appEndWebApiInfo, StatusCodes.Status401Unauthorized, "Status401Unauthorized", ex.Message);
		}

		public static void AddUnauthorizedAccessErrorHeaders(this HttpContext context, Stopwatch sw, Exception ex, AppEndWebApiInfo appEndWebApiInfo)
		{
			AddAppEndStandardHeaders(context, sw, appEndWebApiInfo, StatusCodes.Status401Unauthorized, "Status401Unauthorized", ex.Message);
		}

		public static void AddNotFoundErrorHeaders(this HttpContext context, Stopwatch sw, AppEndWebApiInfo appEndWebApiInfo)
		{
			AddAppEndStandardHeaders(context, sw, appEndWebApiInfo, StatusCodes.Status404NotFound, "Status404NotFound", "NOK");
		}

		private static void AddAppEndStandardHeaders(this HttpContext context, Stopwatch sw, AppEndWebApiInfo appEndWebApiInfo, int statusCode, string statusTitle, string message)
		{
			sw.Stop();
			context.Response.Headers.TryAdd("Server", "AppEnd");

			context.Response.Headers.TryAdd("X-Execution-Path", appEndWebApiInfo.RequestPath);
			context.Response.Headers.TryAdd("X-Execution-Controller", appEndWebApiInfo.ControllerName);
			context.Response.Headers.TryAdd("X-Execution-Action", appEndWebApiInfo.ActionName);
			context.Response.Headers.TryAdd("X-Execution-Duration", sw.ElapsedMilliseconds.ToString());
			context.Response.Headers.TryAdd("X-Execution-User", context.User.Identity?.Name);

			context.Response.Headers.TryAdd("X-Result-StatusCode", statusCode.ToString());
			context.Response.Headers.TryAdd("X-Result-StatusTitle", statusTitle);
			context.Response.Headers.TryAdd("X-Result-Message", message);
		}


	}
}
