using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IntegrationBus.WebApi.OpenApi.OperationFilters
{
	public class ContentTypeOperationFilter : IOperationFilter
	{
		private readonly bool required;
		private readonly string? contentType;
		private readonly string? description;


		public ContentTypeOperationFilter(bool required = false, string? contentType = null, string? responseDescription = null)
		{
			this.required = required;
			this.contentType = contentType;
			this.description = responseDescription;
		}

		/// <inheritdoc/>
		public void Apply(OpenApiOperation operation, OperationFilterContext context)
		{
			operation.Parameters ??= new List<OpenApiParameter>();

			var apiParameter = new OpenApiParameter()
			{
				Description = this.description,
				In = ParameterLocation.Header,
				Name = HeaderNames.ContentType,
				Required = this.required,
				Schema = new OpenApiSchema
				{
					Default = new OpenApiString(this.contentType),
					Type = "string",
				},
			};

			operation.Parameters.Add(apiParameter);
		}
	}
}
