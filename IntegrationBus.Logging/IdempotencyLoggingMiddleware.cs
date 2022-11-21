using IntegrationBus.Logging.Extensions;
using IntegrationBus.Logging.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace IntegrationBus.Logging
{
	public class IdempotencyLoggingMiddleware
	{
		private readonly RequestDelegate next;
		private readonly ILogger<IdempotencyLoggingMiddleware> logger;
		private readonly IdempotencyLoggingOptions options;

		public IdempotencyLoggingMiddleware(RequestDelegate next, ILogger<IdempotencyLoggingMiddleware> logger, IOptions<IdempotencyLoggingOptions> options)
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

			if (httpContext.Request.Headers.TryGetValue(this.options.IdempotencyHeader, out StringValues idempotencyKeyValue))
			{
				using (this.logger.BeginScopeWith((this.options.IdempotencyLogAttribute, idempotencyKeyValue.ToString())))
				{
					await this.next(httpContext);
				}
			}
			else
			{
				await this.next(httpContext);
			}
		}
	}
}
