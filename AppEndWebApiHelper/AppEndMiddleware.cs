using AppEndCommon;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
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

		public async Task InvokeAsync(HttpContext context)
		{
			Stopwatch sw = Stopwatch.StartNew();
			var routeData = context.GetRouteData();

			AppEndWebApiInfo appEndWebApiInfo = context.GetAppEndRpcInfo();

			if (string.IsNullOrEmpty(appEndWebApiInfo.ControllerName) || string.IsNullOrEmpty(appEndWebApiInfo.ControllerName))
			{
				await HandleNotFoundResource(context, sw, appEndWebApiInfo);
				return;
			}

			context.User = context.TurnTokenToUser(_logger);

			try
			{
				CheckAccess(context, appEndWebApiInfo);

				// Check cache if the cache is enabled

				context.Response.OnStarting(() =>
				{
					context.Response.StatusCode = StatusCodes.Status200OK;
					context.AddSuccessHeaders(sw, appEndWebApiInfo);
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
				await HandleUnauthorizedAccessException(context, sw, ex, appEndWebApiInfo);
			}
			catch (Exception ex)
			{
				await HandleException(context, sw, ex, appEndWebApiInfo);
			}
			finally
			{
				// Log success or error for the request
			}
		}

		private void CheckAccess(HttpContext context, AppEndWebApiInfo appEndWebApiInfo)
		{
			var config = AppEndWebApiConfig.FromFile();

			//throw new UnauthorizedAccessException($"Access denied to the {controllerName}::{actionName}.");
		}

		private async Task HandleException(HttpContext context, Stopwatch sw, Exception ex, AppEndWebApiInfo appEndWebApiInfo)
		{
			_logger.LogError(ex, $"Error in {appEndWebApiInfo.ControllerName}::{appEndWebApiInfo.ActionName}: {ex.Message}");
			context.Response.StatusCode = StatusCodes.Status500InternalServerError;
			context.AddInternalErrorHeaders(sw, ex, appEndWebApiInfo);
			context.Response.ContentType = "application/json";
			await context.Response.WriteAsync("{}");
		}
		private async Task HandleUnauthorizedAccessException(HttpContext context, Stopwatch sw, UnauthorizedAccessException ex, AppEndWebApiInfo appEndWebApiInfo)
		{
			_logger.LogError(ex, $"Error in {appEndWebApiInfo.ControllerName}::{appEndWebApiInfo.ActionName}: {ex.Message}");
			context.Response.StatusCode = StatusCodes.Status401Unauthorized;
			context.AddUnauthorizedAccessErrorHeaders(sw, ex, appEndWebApiInfo);
			context.Response.ContentType = "application/json";
			await context.Response.WriteAsync("{}");
		}
		private async Task HandleNotFoundResource(HttpContext context, Stopwatch sw, AppEndWebApiInfo appEndWebApiInfo)
		{
			_logger.LogWarning($"Not found resource: {appEndWebApiInfo.RequestPath}.");
			context.Response.StatusCode = StatusCodes.Status404NotFound;
			context.AddNotFoundErrorHeaders(sw, appEndWebApiInfo);
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