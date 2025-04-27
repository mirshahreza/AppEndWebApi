using AppEndCommon;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Diagnostics;
using System.Security.Authentication;
using System.Security.Claims;
using System.Text.Json;

namespace AppEndWebApiHelper
{
	public class AppEndMiddleware(RequestDelegate next, ILogger<AppEndMiddleware> logger)
	{
		private readonly RequestDelegate _next = next;
		private readonly ILogger<AppEndMiddleware> _logger = logger;

		private AppEndWebApiInfo _appEndWebApiInfo;
		private AppEndWebApiConfig _appEndWebApiConfig;
		

		public async Task InvokeAsync(HttpContext context)
		{
			Log.Information("AppEnd middleware started...");

			Stopwatch sw = Stopwatch.StartNew();
			_appEndWebApiInfo = context.GetAppEndWebApiInfo();

			if (string.IsNullOrEmpty(_appEndWebApiInfo.ControllerName) || string.IsNullOrEmpty(_appEndWebApiInfo.ControllerName))
			{
				await HandleNotFoundResource(context, sw);
				return;
			}

			_appEndWebApiConfig = AppEndWebApiConfigExtensions.ReadConfig(_appEndWebApiInfo);
			context.User = context.TurnTokenToUser(_logger);

			try
			{
				CheckAccess(context);

				// Check cache if the cache is enabled

				context.Response.OnStarting(() =>
				{
					context.Response.StatusCode = StatusCodes.Status200OK;
					context.AddSuccessHeaders(sw, _appEndWebApiInfo);
					return Task.CompletedTask;
				});

				context.Response.OnCompleted(() =>
				{
					return Task.CompletedTask;
				});

				await _next(context);
			}
			catch (UnauthorizedAccessException ex)
			{
				await HandleUnauthorizedAccessException(context, sw, ex);
			}
			catch (Exception ex)
			{
				await HandleException(context, sw, ex);
			}
			finally
			{
				await Log.CloseAndFlushAsync();
			}
		}

		private void CheckAccess(HttpContext context)
		{

			//throw new UnauthorizedAccessException($"Access denied to the {controllerName}::{actionName}.");
		}

		private async Task HandleException(HttpContext context, Stopwatch sw, Exception ex)
		{
			_logger.LogError(ex, $"Error in {_appEndWebApiInfo.ControllerName}::{_appEndWebApiInfo.ActionName}: {ex.Message}");
			context.Response.StatusCode = StatusCodes.Status500InternalServerError;
			context.AddInternalErrorHeaders(sw, ex, _appEndWebApiInfo);
			context.Response.ContentType = "application/json";
			await context.Response.WriteAsync("{}");
		}
		private async Task HandleUnauthorizedAccessException(HttpContext context, Stopwatch sw, UnauthorizedAccessException ex)
		{
			_logger.LogError(ex, $"Error in {_appEndWebApiInfo.ControllerName}::{_appEndWebApiInfo.ActionName}: {ex.Message}");
			context.Response.StatusCode = StatusCodes.Status401Unauthorized;
			context.AddUnauthorizedAccessErrorHeaders(sw, ex, _appEndWebApiInfo);
			context.Response.ContentType = "application/json";
			await context.Response.WriteAsync("{}");
		}
		private async Task HandleNotFoundResource(HttpContext context, Stopwatch sw)
		{
			_logger.LogWarning($"Not found resource: {_appEndWebApiInfo.RequestPath}.");
			context.Response.StatusCode = StatusCodes.Status404NotFound;
			context.AddNotFoundErrorHeaders(sw, _appEndWebApiInfo);
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