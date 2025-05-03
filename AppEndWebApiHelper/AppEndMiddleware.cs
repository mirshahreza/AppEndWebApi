using AppEndCommon;
using Microsoft.AspNetCore.Http;
using Serilog;
using System.Diagnostics;

namespace AppEndWebApiHelper
{
	public class AppEndMiddleware(RequestDelegate next)
	{
		private readonly RequestDelegate _next = next;

		private AppEndWebApiInfo _appEndWebApiInfo;
		private AppEndWebApiConfig _appEndWebApiConfig;
		

		public async Task InvokeAsync(HttpContext context)
		{
			Stopwatch sw = Stopwatch.StartNew();
			_appEndWebApiInfo = context.GetAppEndWebApiInfo();
			bool result = true;
			string message = "";
			string actor = "";
			string rowId = "";

			if (string.IsNullOrEmpty(_appEndWebApiInfo.ControllerName) || string.IsNullOrEmpty(_appEndWebApiInfo.ControllerName))
			{
				sw.Stop();
				await HandleNotFoundResource(context, sw.ElapsedMilliseconds);
				return;
			}

			_appEndWebApiConfig = AppEndWebApiConfigExtensions.ReadConfig(_appEndWebApiInfo);
			context.User = context.TurnTokenToUser();
			actor = context.User.Identity?.Name?.ToString() ?? "";

			try
			{
				CheckAccess(context);

				// todo : Handle Cache if needed (cache configuration / expiration)
				// 

				context.Response.OnStarting(() =>
				{
					context.Response.StatusCode = StatusCodes.Status200OK;
					context.AddSuccessHeaders(sw.ElapsedMilliseconds, _appEndWebApiInfo);
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
				AppEndLogger.LogActivity(_appEndWebApiInfo.ControllerName, _appEndWebApiInfo.ActionName, rowId, result, message, sw.ElapsedMilliseconds.ToIntSafe(), context.GetClientAgent(), context.GetClientIp(), actor);
			}
		}

		private void CheckAccess(HttpContext context)
		{

			//throw new UnauthorizedAccessException($"Access denied to the {controllerName}::{actionName}.");
		}

		private async Task HandleException(HttpContext context, long duration, Exception ex)
		{
			AppEndLogger.LogError($"Error in {_appEndWebApiInfo.ControllerName}::{_appEndWebApiInfo.ActionName}: {ex.Message}");
			context.Response.StatusCode = StatusCodes.Status500InternalServerError;
			context.AddInternalErrorHeaders(duration, ex, _appEndWebApiInfo);
			context.Response.ContentType = "application/json";
			await context.Response.WriteAsync("{}");
		}
		private async Task HandleUnauthorizedAccessException(HttpContext context, long duration, UnauthorizedAccessException ex)
		{
			AppEndLogger.LogError($"Error in {_appEndWebApiInfo.ControllerName}::{_appEndWebApiInfo.ActionName}: {ex.Message}");
			context.Response.StatusCode = StatusCodes.Status401Unauthorized;
			context.AddUnauthorizedAccessErrorHeaders(duration, ex, _appEndWebApiInfo);
			context.Response.ContentType = "application/json";
			await context.Response.WriteAsync("{}");
		}
		private async Task HandleNotFoundResource(HttpContext context, long duration)
		{
			AppEndLogger.LogError($"Error in {_appEndWebApiInfo.ControllerName}::{_appEndWebApiInfo.ActionName}: Not found resource");
			context.Response.StatusCode = StatusCodes.Status404NotFound;
			context.AddNotFoundErrorHeaders(duration, _appEndWebApiInfo);
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