using System.Reflection;
using IntegrationBus.WebApi.OpenApi.OperationFilters;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using IntegrationBus.WebApi.Options;
using IntegrationBus.WebApi.Constants;
using IntegrationBus.WebApi.Extensions;

namespace IntegrationBus.WebApi.OpenApi
{
	public class SwaggerConfigureOptions : IConfigureOptions<SwaggerGenOptions>
	{
		private readonly IApiVersionDescriptionProvider descriptionProvider;
		private readonly Assembly assembly;
		private readonly IdempotencyControlOptions idempotencyControlOptions;

		public SwaggerConfigureOptions(IApiVersionDescriptionProvider descriptionProvider, Assembly assembly, IOptions<IdempotencyControlOptions> idempotencyControlOptions)
		{
			this.descriptionProvider = descriptionProvider ?? throw new ArgumentNullException(nameof(descriptionProvider));
			this.assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
			this.idempotencyControlOptions = idempotencyControlOptions?.Value ?? throw new ArgumentNullException(nameof(idempotencyControlOptions));
		}

		public void Configure(SwaggerGenOptions options)
		{
			options.DescribeAllParametersInCamelCase();
			options.EnableAnnotations();

			// Add the XML comment file for this assembly, so its contents can be displayed.
			options.IncludeXmlCommentsIfExists(this.assembly);

			if (this.idempotencyControlOptions.IdempotencyFilterEnabled ?? false)
			{
				options.OperationFilter<IdempotencyKeyOperationFilter>(this.idempotencyControlOptions.ClientRequestIdHeader);
			}

			options.OperationFilter<ContentTypeOperationFilter>(false, MimeTypes.Application.Json, "The requested Content-Type");

			string assemblyProduct = this.assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? string.Empty;
			string assemblyDescription = this.assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description ?? string.Empty;

			foreach (ApiVersionDescription description in this.descriptionProvider.ApiVersionDescriptions)
			{
				var info = new OpenApiInfo()
				{
					Title = assemblyProduct,
					Description = description.IsDeprecated
						? $"{assemblyDescription} This API version has been deprecated."
						: assemblyDescription,
					Version = description.ApiVersion.ToString()
				};
				options.SwaggerDoc(description.GroupName, info);
			}
		}
	}
}
