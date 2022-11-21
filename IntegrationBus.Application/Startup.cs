using Microsoft.Extensions.DependencyInjection;
using MediatR;
using FluentValidation;
using System;
using IntegrationBus.Shared.Queries.Airport;

namespace IntegrationBus.Application
{
	public static class Startup
	{

		public static IServiceCollection AddIntegrationBusApplication(this IServiceCollection services)
		{
			services.AddMediatR(typeof(Startup));
            services.AddScoped<IValidator<AirportDistanceQuery>, AirportDistanceQueryValidator>();
            return services;
		}
	}
}
