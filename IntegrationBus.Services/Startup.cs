using CorrelationId;
using CorrelationId.DependencyInjection;
using IntegrationBus.Application.Abstract.Services;
using IntegrationBus.Application.Constants;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationBus.Services
{
	public static class Startup
	{
		public static IServiceCollection AddIntegrationBusServices(this IServiceCollection services, string correlationIdHeader)
		{
			services.AddCorrelationIdFluent(correlationIdHeader);
			services.AddHttpContextAccessor()
				.AddScoped<IRuntimeContextAccessor, RuntimeContextAccessor>()
				.AddTransient<IProblemDetailsFactory, AppProblemDetailsFactory>();

			return services;
		}

		public static IApplicationBuilder UseIntegrationBusServices(this IApplicationBuilder appBuilder)
		{
			appBuilder.UseCorrelationId();

			return appBuilder;
		}

		private static IServiceCollection AddCorrelationIdFluent(this IServiceCollection services, string correlationIdHeader)
		{
			//TODO Move correlationIdHeader to options.

			services.AddDefaultCorrelationId(c =>
			{
				c.CorrelationIdGenerator = () => Guid.NewGuid().ToString();
				c.AddToLoggingScope = true;
				c.LoggingScopeKey = Logs.CorrelationId;
				c.EnforceHeader = false;
				c.IgnoreRequestHeader = true;
				c.IncludeInResponse = true;
				c.RequestHeader = correlationIdHeader;
				c.ResponseHeader = correlationIdHeader;
				c.UpdateTraceIdentifier = false;
			});

			return services;
		}
	}
}
