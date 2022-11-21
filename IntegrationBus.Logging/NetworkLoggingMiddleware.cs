using IntegrationBus.Logging.Constants;
using IntegrationBus.Logging.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace IntegrationBus.Logging
{
	public class NetworkLoggingMiddleware
	{
		private readonly RequestDelegate next;
		private readonly ILogger<NetworkLoggingMiddleware> logger;

		public NetworkLoggingMiddleware(RequestDelegate next, ILogger<NetworkLoggingMiddleware> logger)
		{
			this.next = next ?? throw new ArgumentNullException(nameof(next));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public async Task InvokeAsync(HttpContext httpContext)
		{
			HttpContext context = httpContext ?? throw new ArgumentNullException(nameof(httpContext));

			string clientIp;

			if (context.Request.Headers.TryGetValue("X-Original-For", out StringValues proxyForwardedValue))
			{
				clientIp = GetIpFromHeaderString(proxyForwardedValue);
			}
			else
			{
				string directValue = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
				clientIp = !string.IsNullOrWhiteSpace(directValue)
					? directValue
					: "0.0.0.0";
			}

			using (this.logger.BeginScopeWith((LogKeys.ClientIp, clientIp)))
			{
				await this.next(context);
			}
		}

		private static string GetIpFromHeaderString(StringValues ipAddresses)
		{
			string[] addresses = ipAddresses.LastOrDefault().Split(',');

			string ipAddress = string.Empty;

			if (addresses.Length != 0)
			{
				ipAddress = addresses[0].Contains(":", StringComparison.Ordinal)
					? addresses[0].Substring(0, addresses[0].LastIndexOf(":", StringComparison.Ordinal))
					: addresses[0];
			}

			return ipAddress;
		}
	}
}
