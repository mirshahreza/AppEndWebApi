using AppEndCommon;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace AppEndWebApiHelper
{
	public static class AppEndExtenssions
	{
		public static string NobodyUserName = "nobody";
		public static string AuthenticationType = "AppEnd";

		public static bool IsPostFace(this HttpContext context)
		{
			return context.Request.Method == HttpMethods.Post || context.Request.Method == HttpMethods.Put || context.Request.Method == HttpMethods.Patch;
		}

		private static ClaimsPrincipal ToClaimsPrincipal(this HttpContext context)
		{
			if (!context.Request.Headers.TryGetValue("token", out Microsoft.Extensions.Primitives.StringValues value)) return GetNobodyClaimsPrincipal();
			try
			{
				string token = value.ToStringEmpty().Replace("bearer ", "").Decode(ProjectHelpers.EncriptionSecret);
				var jo = token.ToJsonObjectByBuiltIn();
				// todo : check token expiration
				var claims = new List<Claim>();

				if (jo.TryGetPropertyValue("Id", out JsonNode? Id)) claims.Add(new Claim("Id", Id.ToStringEmpty()));
				if (jo.TryGetPropertyValue("UserName", out JsonNode? UserName)) claims.Add(new Claim(ClaimTypes.Name, UserName.ToStringEmpty()));
				if (jo.TryGetPropertyValue("Roles", out JsonNode? Role)) claims.Add(new Claim(ClaimTypes.Role, Role.ToStringEmpty()));
				if (jo.TryGetPropertyValue("UserData", out JsonNode? UserData)) claims.Add(new Claim(ClaimTypes.UserData, UserData.ToStringEmpty()));

				var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, AuthenticationType));
				return principal;
			}
			catch
			{
				AppEndLogger.LogWarning($"Invalid token tried !!!");
				return GetNobodyClaimsPrincipal();
			}
		}

		public static UserServerObject ToUserServerObject(this HttpContext context)
		{
			ClaimsPrincipal claimsPrincipal = context.ToClaimsPrincipal();

			if (claimsPrincipal.Identity is null || claimsPrincipal.Identity.Name is null) return GetNobodyUserServerObject();
			string key = $"USO::{claimsPrincipal.Identity.Name}";
			if (ExtMemory.SharedMemoryCache.TryGetValue(key, out UserServerObject? user) && user is not null) return user;
			List<string> roles = claimsPrincipal.FindFirst(ClaimTypes.Role)?.Value.ToStringEmpty().Split(",").ToList() ?? [];
			user = new()
			{
				Id = claimsPrincipal.FindFirst("Id")?.Value.ToIntSafe(0) ?? 0,
				UserName = claimsPrincipal.FindFirst(ClaimTypes.Name)?.Value.ToStringEmpty() ?? NobodyUserName,
				Roles = roles.AsParallel().Select(r => new Role
				{
					Id = r.ToIntSafe(),
					RoleName = "", // todo : must calculate
					Data = [] // todo : must calculate
				}).ToList(),
				Data = claimsPrincipal.FindFirst(ClaimTypes.UserData)?.Value.ToJsonObjectByBuiltIn() ?? [],
				AllowedActions = [],
				IsPubKey = false // todo : must calculate
			};
			ExtMemory.SharedMemoryCache.Set(key, user, TimeSpan.FromMinutes(10));
			return user;
		}


		public static ClaimsPrincipal GetNobodyClaimsPrincipal()
		{
			var claims = new List<Claim> { new("Id", "0"), new(ClaimTypes.Name, NobodyUserName) };
			return new ClaimsPrincipal(new ClaimsIdentity(claims, AuthenticationType));
		}

		public static UserServerObject GetNobodyUserServerObject()
		{
			return new() { Id = 0, UserName = NobodyUserName, Roles = [], Data = [] };
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
