using AppEndCommon;
using Microsoft.AspNetCore.Http;
using Serilog;
using System.Diagnostics;

namespace AppEndWebApiHelper
{
	public class AppEndMiddleware(RequestDelegate next)
	{
		private readonly RequestDelegate _next = next;

		private ApiInfo _apiInfo;
		private ApiConf _apiConf;
		private UserServerObject _user;
		

		public async Task InvokeAsync(HttpContext context)
		{
			Stopwatch sw = Stopwatch.StartNew();
			_apiInfo = context.GetAppEndWebApiInfo();
			bool result = true;
			string message = "";
			string actorUserName = "";
			string rowId = "";

			if (string.IsNullOrEmpty(_apiInfo.ControllerName) || string.IsNullOrEmpty(_apiInfo.ControllerName))
			{
				sw.Stop();
				await HandleNotFoundResource(context, sw.ElapsedMilliseconds);
				return;
			}

			_apiConf = _apiInfo.ReadConfig();
			context.User = context.TurnTokenToClaimsPrincipal();
			_user = context.User.ToUserServerObject();
			actorUserName = context.User.Identity?.Name?.ToString() ?? "";

			try
			{
				if (!HasAccess(context)) throw new UnauthorizedAccessException($"Access denied to the {_apiInfo.ControllerName}::{_apiInfo.ActionName}.");



				// todo : Handle Cache if needed (cache configuration / expiration)
				// 

				context.Response.OnStarting(() =>
				{
					context.Response.StatusCode = StatusCodes.Status200OK;
					context.AddSuccessHeaders(sw.ElapsedMilliseconds, _apiInfo);
					return Task.CompletedTask;
				});

				context.Response.OnCompleted(() =>
				{
					sw.Stop();
					rowId = context.Items["RowId"]?.ToString() ?? "";
					return Task.CompletedTask;
				});

				await _next(context);
			}
			catch (UnauthorizedAccessException ex)
			{
				sw.Stop();
				result = false;
				message = ex.Message;
				rowId = context.Items["RowId"]?.ToString() ?? "";
				await HandleUnauthorizedAccessException(context, sw.ElapsedMilliseconds, ex);
			}
			catch (Exception ex)
			{
				sw.Stop();
				result = false;
				message = ex.Message;
				rowId = context.Items["RowId"]?.ToString() ?? "";
				await HandleException(context, sw.ElapsedMilliseconds, ex);
			}
			finally
			{
				sw.Stop();
				rowId = context.Items["RowId"]?.ToString() ?? "";
				if (_apiConf.LogEnabled)
					AppEndLogger.LogActivity(_apiInfo.ControllerName, _apiInfo.ActionName, rowId, result, message, sw.ElapsedMilliseconds.ToIntSafe(), 
						context.GetClientAgent(), context.GetClientIp(), actorUserName);
			}
		}

		private bool HasAccess(HttpContext context)
		{
			if (_user.IsPubKey) return true;

			if (_apiConf.CheckAccessLevel == CheckAccessLevel.OpenForAllUsers) return true;
			if (_apiConf.CheckAccessLevel == CheckAccessLevel.OpenForAuthenticatedUsers && !_user.Id.Equals("-1")) return true;

			// check if is pubkey
			if (_user.IsPubKey) return true;
			if (_user.Roles is not null && _user.Roles.Any(i => i.IsPubKey == true)) return true;

			// check for denies
			if (_apiConf.DeniedUsers.ContainsIgnoreCase(_user.Id)) return false;
			if (_apiConf.DeniedRoles?.Count > 0 && _user.Roles?.Count > 0 && _apiConf.DeniedRoles.HasIntersect([.. _user.Roles?.Select(i => i.Id) ?? []])) return false;

			// check for access
			if (_apiConf.AllowedRoles.HasIntersect([.. _user.Roles?.Select(i => i.Id) ?? []])) return true;
			if (_apiConf.AllowedUsers.ContainsIgnoreCase(_user.Id)) return true;

			return false;
		}

		private async Task HandleException(HttpContext context, long duration, Exception ex)
		{
			AppEndLogger.LogError($"Error in {_apiInfo.ControllerName}::{_apiInfo.ActionName}: {ex.Message}");
			context.Response.StatusCode = StatusCodes.Status500InternalServerError;
			context.AddInternalErrorHeaders(duration, ex, _apiInfo);
			context.Response.ContentType = "application/json";
			await context.Response.WriteAsync("{}");
		}
		private async Task HandleUnauthorizedAccessException(HttpContext context, long duration, UnauthorizedAccessException ex)
		{
			AppEndLogger.LogError($"Error in {_apiInfo.ControllerName}::{_apiInfo.ActionName}: {ex.Message}");
			context.Response.StatusCode = StatusCodes.Status401Unauthorized;
			context.AddUnauthorizedAccessErrorHeaders(duration, ex, _apiInfo);
			context.Response.ContentType = "application/json";
			await context.Response.WriteAsync("{}");
		}
		private async Task HandleNotFoundResource(HttpContext context, long duration)
		{
			AppEndLogger.LogError($"Error in {_apiInfo.ControllerName}::{_apiInfo.ActionName}: Not found resource");
			context.Response.StatusCode = StatusCodes.Status404NotFound;
			context.AddNotFoundErrorHeaders(duration, _apiInfo);
			context.Response.ContentType = "application/json";
			await context.Response.WriteAsync("{}");
		}

	}
}


// Controlling the rate limit

//string requestBody = "";
//if (context.Request.Method == HttpMethods.Post || context.Request.Method == HttpMethods.Put || context.Request.Method == HttpMethods.Patch)
//{
//	context.Request.EnableBuffering();
//	using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
//	{
//		requestBody = await reader.ReadToEndAsync();
//	}
//	context.Request.Body.Position = 0;
//}