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

			string requestPath = context.Request.Path.ToString();

			Tuple<string, string> caNames = context.GetControllerAndActionNames();

			if (string.IsNullOrEmpty(caNames.Item1) || string.IsNullOrEmpty(caNames.Item2))
			{
				await HandleNotFoundResource(context, sw, requestPath);
				return;
			}

			context.User = context.TurnTokenToUser(_logger);

			try
			{
				CheckAccess(context, caNames.Item1, caNames.Item2);

				// Check cache if the cache is enabled

				context.Response.OnStarting(() =>
				{
					context.Response.StatusCode = StatusCodes.Status200OK;
					context.AddSuccessHeaders(sw, requestPath, caNames.Item1, caNames.Item2);
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
				await HandleUnauthorizedAccessException(context, sw, ex, requestPath, caNames.Item1, caNames.Item2);
			}
			catch (Exception ex)
			{
				await HandleException(context, sw, ex, requestPath, caNames.Item1, caNames.Item2);
			}
			finally
			{
				// Log success or error for the request
			}
		}

		private void CheckAccess(HttpContext context, string controllerName, string actionName)
		{
			//throw new UnauthorizedAccessException($"Access denied to the {controllerName}::{actionName}.");
		}

		private async Task HandleException(HttpContext context, Stopwatch sw, Exception ex, string requestPath, string controllerName, string actionName)
		{
			_logger.LogError(ex, $"Error in {controllerName}::{actionName}: {ex.Message}");
			context.Response.StatusCode = StatusCodes.Status500InternalServerError;
			context.AddInternalErrorHeaders(sw, ex, requestPath, controllerName, actionName);
			context.Response.ContentType = "application/json";
			await context.Response.WriteAsync("{}");
		}
		private async Task HandleUnauthorizedAccessException(HttpContext context, Stopwatch sw, UnauthorizedAccessException ex, string requestPath, string controllerName, string actionName)
		{
			_logger.LogError(ex, $"Error in {controllerName}::{actionName}: {ex.Message}");
			context.Response.StatusCode = StatusCodes.Status401Unauthorized;
			context.AddUnauthorizedAccessErrorHeaders(sw, ex, requestPath, controllerName, actionName);
			context.Response.ContentType = "application/json";
			await context.Response.WriteAsync("{}");
		}
		private async Task HandleNotFoundResource(HttpContext context, Stopwatch sw, string requestPath)
		{
			_logger.LogWarning($"Not found resource: {requestPath}.");
			context.Response.StatusCode = StatusCodes.Status404NotFound;
			context.AddNotFoundErrorHeaders(sw, requestPath);
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