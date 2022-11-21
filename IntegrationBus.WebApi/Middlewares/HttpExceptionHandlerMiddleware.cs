using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using IntegrationBus.Application.Abstract.Services;
using IntegrationBus.Application.Exceptions;
using IntegrationBus.Logging.Constants;
using IntegrationBus.Logging.Extensions;
using Microsoft.AspNetCore.Mvc;
using IntegrationBus.WebApi.Constants;
using System.Diagnostics;

namespace IntegrationBus.WebApi.Middlewares
{
	public class HttpExceptionHandlerMiddleware
	{
		private readonly RequestDelegate next;
		private readonly ILogger<HttpExceptionHandlerMiddleware> logger;
		private readonly IProblemDetailsFactory problemDetailsFactory;
		private readonly IHostApplicationLifetime applicationLifetime;

		public HttpExceptionHandlerMiddleware(RequestDelegate next, ILogger<HttpExceptionHandlerMiddleware> logger, IProblemDetailsFactory problemDetailsFactory, IHostApplicationLifetime applicationLifetime)
		{
			this.next = next ?? throw new ArgumentNullException(nameof(next));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.problemDetailsFactory = problemDetailsFactory ?? throw new ArgumentNullException(nameof(problemDetailsFactory));
			this.applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
		}

		public async Task InvokeAsync(HttpContext httpContext)
		{
			try
			{
				await this.next(httpContext);
			}
			catch (ApplicationExceptionBase ex)
			{
				using (this.logger.BeginScopeWith((LogKeys.ErrorCode, (int)ex.ErrorCode)))
				{
					this.logger.LogWarning(ex, ex.Message);
				}

				int exceptionStatusCode;
				int errorCode = (int)ex.ErrorCode;

				switch (ex)
				{
					default:
					{
						exceptionStatusCode = StatusCodes.Status500InternalServerError;
						break;
					}
				}

				ProblemDetails problemDetails =
					this.problemDetailsFactory.CreateProblemDetails(
						httpContext,
						exceptionStatusCode,
						ex.Message,
						null,
						null,
						httpContext.Request.Path);

				problemDetails.Extensions.Add(nameof(errorCode), errorCode);

				httpContext.Response.ContentType = MimeTypes.Application.ProblemJson;
				httpContext.Response.StatusCode = exceptionStatusCode;

				await JsonSerializer.SerializeAsync(
					httpContext.Response.Body,
					problemDetails,
					this.GetJsonSerializerOptions(),
					this.applicationLifetime.ApplicationStopping);
			}
			catch (OperationCanceledException ex)
			{
				if (!httpContext.RequestAborted.IsCancellationRequested)
				{
					ProblemDetails problemDetails =
						this.problemDetailsFactory.CreateProblemDetails(
							httpContext,
							StatusCodes.Status500InternalServerError,
							ex.Message,
							null,
							ex.Demystify().StackTrace,
							httpContext.Request.Path);

					httpContext.Response.ContentType = MimeTypes.Application.ProblemJson;
					httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

					await JsonSerializer.SerializeAsync(
						httpContext.Response.Body,
						problemDetails,
						this.GetJsonSerializerOptions(),
						this.applicationLifetime.ApplicationStopping);
				}
				else
				{
					this.logger.LogWarning(ex, "Operation canceled.");
				}
			}
			catch (Exception ex)
			{
				this.logger.LogError(ex, "Unhanded exception.");

				ProblemDetails problemDetails =
					this.problemDetailsFactory.CreateProblemDetails(
						httpContext,
						StatusCodes.Status500InternalServerError,
						ex.Message,
						null,
						ex.Demystify().StackTrace,
						httpContext.Request.Path);

				httpContext.Response.ContentType = MimeTypes.Application.ProblemJson;
				httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

				await JsonSerializer.SerializeAsync(
					httpContext.Response.Body,
					problemDetails,
					this.GetJsonSerializerOptions(),
					this.applicationLifetime.ApplicationStopping);
			}
		}

		private JsonSerializerOptions GetJsonSerializerOptions()
		{
			JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions();
			jsonSerializerOptions.WriteIndented = false;
			jsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
			jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
			jsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
			jsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
			jsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);

			return jsonSerializerOptions;
		}
	}
}
