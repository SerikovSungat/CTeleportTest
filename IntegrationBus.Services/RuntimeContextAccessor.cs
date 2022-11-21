using System.Diagnostics;
using CorrelationId.Abstractions;
using IntegrationBus.Application.Abstract.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace IntegrationBus.Services
{
	public class RuntimeContextAccessor : IRuntimeContextAccessor
	{
		private readonly ILogger<RuntimeContextAccessor> logger;
		private readonly IHttpContextAccessor httpContextAccessor;
		private readonly ICorrelationContextAccessor correlationContextAccessor;

		public RuntimeContextAccessor(ILogger<RuntimeContextAccessor> logger, IHttpContextAccessor httpContextAccessor, ICorrelationContextAccessor correlationContextAccessor)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
			this.correlationContextAccessor = correlationContextAccessor ?? throw new ArgumentNullException(nameof(correlationContextAccessor));
		}

		public Guid GetCorrelationId()
		{
			if (this.correlationContextAccessor.CorrelationContext is null)
			{
				return Guid.Empty;
			}

			if (Guid.TryParse(this.correlationContextAccessor.CorrelationContext.CorrelationId, out Guid correlationId))
			{
				return correlationId;
			}

			return Guid.Empty;
		}

		public string GetTraceId()
		{
			string traceId = Activity.Current?.Id ?? this.httpContextAccessor.HttpContext.TraceIdentifier;
			return traceId;
		}
	}
}
