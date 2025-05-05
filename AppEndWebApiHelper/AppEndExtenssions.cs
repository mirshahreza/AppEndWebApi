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
		public static ClaimsPrincipal ToClaimsPrincipal(this HttpContext context)
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
						new("Id", ""),
						new(ClaimTypes.Name, "TestUser"),
						new(ClaimTypes.Role, "User"),
						new(ClaimTypes.UserData, "CustomValue")
					};
					var identity = new ClaimsIdentity(claims, "AppEndAuth");
					return new ClaimsPrincipal(identity);
				}
				catch
				{
					AppEndLogger.LogWarning($"Invalid token tried !!!");
					return GetNobodyUser();
				}
			}
		}

		public static UserServerObject ToUserServerObject(this ClaimsPrincipal claimsPrincipal)
		{
			UserServerObject user = new()
			{
				Id = claimsPrincipal.FindFirst("Id")?.Value.ToIntSafe(0) ?? 0,
				UserName = claimsPrincipal.FindFirst(ClaimTypes.Name)?.Value.ToStringEmpty() ?? "nobody"
			};
			if (claimsPrincipal.Identity is not null && claimsPrincipal.Identity.IsAuthenticated)
			{
				List<string> roles = claimsPrincipal.FindFirst(ClaimTypes.Name)?.Value.ToStringEmpty().Split(",").ToList() ?? [];
				roles.ForEach(r => user.Roles.Add(new Role() { Id = r.ToIntSafe(), RoleName = "", Data = claimsPrincipal.FindFirst(ClaimTypes.UserData)?.Value.ToJsonObjectByBuiltIn() }));
			}
			else
			{
				user.Roles = [];
				user.Data = [];
			}
			return user;
		}


		public static ClaimsPrincipal GetNobodyUser()
		{
			var claims = new List<Claim>
					{
						new Claim("Id", "0"),
						new Claim(ClaimTypes.Name, "nobody")
					};
			var identity = new ClaimsIdentity(claims, "AppEndAuth");
			return new ClaimsPrincipal(identity);
		}

		public static ApiInfo GetAppEndWebApiInfo(this HttpContext context)
		{
			var routeData = context.GetRouteData();
			return new ApiInfo(context.Request.Path.ToString(), routeData.Values["controller"].ToStringEmpty(), routeData.Values["action"].ToStringEmpty());
		}

		public static void AddSuccessHeaders(this HttpContext context, long duration, ApiInfo appEndWebApiInfo)
		{
			AddAppEndStandardHeaders(context, duration, appEndWebApiInfo, StatusCodes.Status200OK, "Status200OK", "OK");
		}

		public static void AddInternalErrorHeaders(this HttpContext context, long duration, Exception ex, ApiInfo appEndWebApiInfo)
		{
			AddAppEndStandardHeaders(context, duration, appEndWebApiInfo, StatusCodes.Status401Unauthorized, "Status401Unauthorized", ex.Message);
		}

		public static void AddUnauthorizedAccessErrorHeaders(this HttpContext context, long duration, Exception ex, ApiInfo appEndWebApiInfo)
		{
			AddAppEndStandardHeaders(context, duration, appEndWebApiInfo, StatusCodes.Status401Unauthorized, "Status401Unauthorized", ex.Message);
		}

		public static void AddNotFoundErrorHeaders(this HttpContext context, long duration, ApiInfo appEndWebApiInfo)
		{
			AddAppEndStandardHeaders(context, duration, appEndWebApiInfo, StatusCodes.Status404NotFound, "Status404NotFound", "NOK");
		}

		private static void AddAppEndStandardHeaders(this HttpContext context, long duration, ApiInfo appEndWebApiInfo, int statusCode, string statusTitle, string message)
		{
			context.Response.Headers.TryAdd("Server", "AppEnd");

			context.Response.Headers.TryAdd("X-Execution-Path", appEndWebApiInfo.RequestPath);
			context.Response.Headers.TryAdd("X-Execution-Controller", appEndWebApiInfo.ControllerName);
			context.Response.Headers.TryAdd("X-Execution-Action", appEndWebApiInfo.ActionName);
			context.Response.Headers.TryAdd("X-Execution-Duration", duration.ToString());
			context.Response.Headers.TryAdd("X-Execution-User", context.User.Identity?.Name);

			context.Response.Headers.TryAdd("X-Result-StatusCode", statusCode.ToString());
			context.Response.Headers.TryAdd("X-Result-StatusTitle", statusTitle);
			context.Response.Headers.TryAdd("X-Result-Message", message);
		}

		public static string GetClientIp(this HttpContext context)
		{
			return context.Connection.RemoteIpAddress.MapToIPv4().ToString();
		}
		public static string GetClientAgent(this HttpContext context)
		{
			return context.Request.Headers["User-Agent"].ToString();
		}



	}
}
