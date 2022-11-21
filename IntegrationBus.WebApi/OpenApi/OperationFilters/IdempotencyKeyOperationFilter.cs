using System.Reflection;
using IntegrationBus.WebApi.Middlewares.ResourceFilter;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IntegrationBus.WebApi.OpenApi.OperationFilters
{
	//TODO Not implemented.
	public class IdempotencyKeyOperationFilter : IOperationFilter
	{
		private readonly string parameterName;

		public IdempotencyKeyOperationFilter(string parameterName = "Idempotency-Key")
		{
			if (string.IsNullOrWhiteSpace(parameterName))
			{
				throw new ArgumentException(nameof(parameterName));
			}

			this.parameterName = parameterName;
		}

		public void Apply(OpenApiOperation operation, OperationFilterContext context)
		{
			IEnumerable<ServiceFilterAttribute> attributes = GetControllerAndActionAttributes<ServiceFilterAttribute>(context);
			IEnumerable<ServiceFilterAttribute> idempotencyAttributes = attributes.Where(e => e.ServiceType.Name == nameof(IdempotencyFilterAttribute));

			bool shouldBeIdempotent = idempotencyAttributes.Any();

			if (!shouldBeIdempotent)
			{
				return;
			}

			if (operation.Parameters is null)
			{
				operation.Parameters = new List<OpenApiParameter>();
			}

			//TODO Use SwaggerResponseDescriptions.
			operation.Parameters.Add(
				new OpenApiParameter()
				{
					Description = "Идентификатор запроса, используется для контроля идемпотентности.",
					In = ParameterLocation.Header,
					Name = this.parameterName,
					Required = true,
					Schema = new OpenApiSchema
					{
						Default = new OpenApiString(Guid.NewGuid().ToString()),
						Type = "string",
					},
				});
		}

		private static IEnumerable<T> GetControllerAndActionAttributes<T>(OperationFilterContext context) where T : Attribute
		{
			IEnumerable<T> controllerAttributes = context.MethodInfo.DeclaringType?.GetTypeInfo().GetCustomAttributes<T>() ?? new List<T>();
			IEnumerable<T> actionAttributes = context.MethodInfo.GetCustomAttributes<T>();
			var result = new List<T>(controllerAttributes);
			result.AddRange(actionAttributes);

			return result;
		}
	}
}
