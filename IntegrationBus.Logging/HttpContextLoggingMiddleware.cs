using System.Globalization;
using System.Text;
using IntegrationBus.Logging.Constants;
using IntegrationBus.Logging.Extensions;
using IntegrationBus.Logging.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IntegrationBus.Logging
{
	public class HttpContextLoggingMiddleware
	{
		private readonly RequestDelegate next;
		private readonly ILogger<HttpContextLoggingMiddleware> logger;
		private readonly HttpContextLoggingOptions options;

		public HttpContextLoggingMiddleware(RequestDelegate next, ILogger<HttpContextLoggingMiddleware> logger, IOptions<HttpContextLoggingOptions> options)
		{
			this.next = next ?? throw new ArgumentNullException(nameof(next));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.options = options.Value ?? throw new ArgumentNullException(nameof(options));
		}

		public async Task InvokeAsync(HttpContext httpContext)
		{
			if (httpContext is null)
			{
				throw new ArgumentNullException(nameof(httpContext));
			}

			CancellationToken cancellationToken = httpContext.RequestAborted;

			bool skipLogging = this.options.SkipPaths.Any(p => p.Value == httpContext.Request.Path);

			if (skipLogging)
			{
				await this.next(httpContext);
				return;
			}

			Dictionary<string, object> requestHeaders = GetValidHeaders(httpContext.Request.Headers, LogKeys.RequestHeaders);

			foreach (string key in this.options.SkipRequestHeaders)
			{
				requestHeaders.Remove($"{LogKeys.RequestHeaders}.{key}");
			}

			using (this.logger.BeginScope(requestHeaders))
			{
				using (this.logger.BeginScopeWith((LogKeys.RequestProtocol, httpContext.Request.Protocol),
						   (LogKeys.RequestScheme, httpContext.Request.Scheme),
						   (LogKeys.RequestHost, httpContext.Request.Host.Value),
						   (LogKeys.RequestMethod, httpContext.Request.Method),
						   (LogKeys.RequestPath, httpContext.Request.Path),
						   (LogKeys.RequestQuery, httpContext.Request.QueryString),
						   (LogKeys.RequestPathAndQuery, GetFullPath(httpContext))))
				{
					if (this.options.LogRequestBody)
					{
						httpContext.Request.EnableBuffering();
						Stream body = httpContext.Request.Body;
						byte[] buffer = new byte[Convert.ToInt32(httpContext.Request.ContentLength, CultureInfo.InvariantCulture)];
						await httpContext.Request.Body.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
						string initialRequestBody = Encoding.UTF8.GetString(buffer);
						body.Seek(0, SeekOrigin.Begin);
						httpContext.Request.Body = body;

						if (this.options.MaxBodyLength > 0 && initialRequestBody.Length > this.options.MaxBodyLength)
						{
							initialRequestBody = initialRequestBody.Substring(0, this.options.MaxBodyLength);
						}

						using (this.logger.BeginScopeWith((LogKeys.RequestBody, initialRequestBody)))
						{
							this.logger.LogInformation("HTTP request received.");
						}
					}
					else
					{
						this.logger.LogInformation("HTTP request received.");
					}
				}
			}

			if (this.options.LogResponseBody)
			{
				await using (var responseBodyMemoryStream = new MemoryStream())
				{
					Stream originalResponseBodyReference = httpContext.Response.Body;
					httpContext.Response.Body = responseBodyMemoryStream;

					await this.next(httpContext);

					httpContext.Response.Body.Seek(0, SeekOrigin.Begin);

					string responseBody;

					using (var sr = new StreamReader(httpContext.Response.Body))
					{
						responseBody = await sr.ReadToEndAsync();
						httpContext.Response.Body.Seek(0, SeekOrigin.Begin);

						string endResponseBody = (this.options.MaxBodyLength > 0 && responseBody.Length > this.options.MaxBodyLength)
							? responseBody.Substring(0, this.options.MaxBodyLength)
							: responseBody;

						Dictionary<string, object> responseHeaders = GetValidHeaders(httpContext.Response.Headers, LogKeys.ResponseHeaders);

						foreach (string key in this.options.SkipResponseHeaders)
						{
							responseHeaders.Remove($"{LogKeys.ResponseHeaders}.{key}");
						}

						using (this.logger.BeginScope(responseHeaders))
						{
							using (this.logger.BeginScopeWith((LogKeys.StatusCode, httpContext.Response.StatusCode),
									   (LogKeys.ResponseBody, endResponseBody),
									   (LogKeys.RequestProtocol, httpContext.Request.Protocol),
									   (LogKeys.RequestScheme, httpContext.Request.Scheme),
									   (LogKeys.RequestHost, httpContext.Request.Host.Value),
									   (LogKeys.RequestMethod, httpContext.Request.Method),
									   (LogKeys.RequestPath, httpContext.Request.Path),
									   (LogKeys.RequestQuery, httpContext.Request.QueryString),
									   (LogKeys.RequestPathAndQuery, GetFullPath(httpContext)),
									   (LogKeys.RequestAborted, httpContext.RequestAborted.IsCancellationRequested)))
							{
								this.logger.LogInformation("HTTP request handled.");
							}
						}

						await responseBodyMemoryStream.CopyToAsync(originalResponseBodyReference, cancellationToken);
					}
				}
			}
			else
			{
				await this.next(httpContext);

				Dictionary<string, object> responseHeaders = GetValidHeaders(httpContext.Response.Headers, LogKeys.ResponseHeaders);

				foreach (string key in this.options.SkipResponseHeaders)
				{
					responseHeaders.Remove($"{LogKeys.ResponseHeaders}.{key}");
				}

				using (this.logger.BeginScope(responseHeaders))
				{
					using (this.logger.BeginScopeWith((LogKeys.StatusCode, httpContext.Response.StatusCode),
							   (LogKeys.RequestProtocol, httpContext.Request.Protocol),
							   (LogKeys.RequestScheme, httpContext.Request.Scheme),
							   (LogKeys.RequestHost, httpContext.Request.Host.Value),
							   (LogKeys.RequestMethod, httpContext.Request.Method),
							   (LogKeys.RequestPath, httpContext.Request.Path),
							   (LogKeys.RequestQuery, httpContext.Request.QueryString),
							   (LogKeys.RequestPathAndQuery, GetFullPath(httpContext)),
							   (LogKeys.RequestAborted, httpContext.RequestAborted.IsCancellationRequested)))
					{
						this.logger.LogInformation("HTTP request handled.");
					}
				}
			}
		}

		private static Dictionary<string, object> ConvertHeadersToDictionaryWithPrefix(IHeaderDictionary headers, string keyPrefix)
		{
			return headers.ToDictionary(h => $"{keyPrefix}.{h.Key}", h => h.Value.ToString() as object);
		}

		private static Dictionary<string, object> GetValidHeaders(IHeaderDictionary headers, string headerKeyPrefix)
		{
			Dictionary<string, object> validHeaders = ConvertHeadersToDictionaryWithPrefix(headers, headerKeyPrefix);

			IEnumerable<string> emptyHeaders = validHeaders.Where(e => e.Value is null).Select(s => s.Key);

			foreach (string key in emptyHeaders)
			{
				validHeaders.Remove(key);
			}

			return validHeaders;
		}

		private static string GetFullPath(HttpContext httpContext)
		{
			/*
				In some cases, like when running integration tests with WebApplicationFactory<T>
				the RawTarget returns an empty string instead of null, in that case we can't use
				?? as fallback.
			*/
			string requestPath = httpContext.Features.Get<IHttpRequestFeature>()?.RawTarget ?? string.Empty;

			if (string.IsNullOrWhiteSpace(requestPath))
			{
				requestPath = httpContext.Request.Path.ToString();
			}

			return requestPath;
		}
	}
}
