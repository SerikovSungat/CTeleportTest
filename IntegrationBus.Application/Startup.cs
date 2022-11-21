using Microsoft.Extensions.DependencyInjection;
using MediatR;

namespace IntegrationBus.Application
{
	public static class Startup
	{

		public static IServiceCollection AddIntegrationBusApplication(this IServiceCollection services)
		{

            services.AddMediatR(typeof(Startup));
            return services;
		}
	}
}
