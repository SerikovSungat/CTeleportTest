using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using IntegrationBus.WebApi.Constants;
using IntegrationBus.WebApi.Middlewares;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;
using IntegrationBus.WebApi.Options;

namespace IntegrationBus.WebApi.Extensions
{
	public static class ApplicationBuilderExtensions
	{
		public static IMvcBuilder AddCustomJsonOptions(this IMvcBuilder builder, IWebHostEnvironment webHostEnvironment)
		{
			builder.AddJsonOptions(c =>
			{
				// https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-migrate-from-newtonsoft-how-to?pivots=dotnet-6-0
				JsonSerializerOptions jsonSerializerOptions = c.JsonSerializerOptions;

				jsonSerializerOptions.WriteIndented = webHostEnvironment.IsDevelopment();

				jsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
				jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
				jsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
				jsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
				jsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
			});

			return builder;
		}

		public static IMvcBuilder AddCustomMvcOptions(this IMvcBuilder builder, IConfiguration configuration)
		{
			builder.AddMvcOptions(options =>
			{
				// Remove plain text (text/plain) output formatter.
				options.OutputFormatters.RemoveType<StringOutputFormatter>();

				// Configure System.Text JSON.
				MediaTypeCollection jsonSystemInputFormatterMediaTypes = options
						.InputFormatters
						.OfType<SystemTextJsonInputFormatter>()
						.First()
						.SupportedMediaTypes;
				MediaTypeCollection jsonSystemOutputFormatterMediaTypes = options
					.OutputFormatters
					.OfType<SystemTextJsonOutputFormatter>()
					.First()
					.SupportedMediaTypes;

				// Remove JSON text (text/json) media type from the system JSON input and output formatters.
				jsonSystemInputFormatterMediaTypes.Remove(MimeTypes.Text.Json);
				jsonSystemOutputFormatterMediaTypes.Remove(MimeTypes.Text.Json);

				// Add RESTful JSON media type (application/vnd.restful+json) to the JSON input and output formatters.
				// See http://restfuljson.org/
				jsonSystemInputFormatterMediaTypes.Insert(0, MimeTypes.Application.RestfulJson);
				jsonSystemOutputFormatterMediaTypes.Insert(0, MimeTypes.Application.RestfulJson);

				// Add Problem Details media type (application/problem+json) to the JSON output formatters.
				// See https://tools.ietf.org/html/rfc7807
				jsonSystemOutputFormatterMediaTypes.Insert(0, MimeTypes.Application.ProblemJson);

				// Returns a 406 Not Acceptable if the MIME type in the Accept HTTP header is not valid.
				options.ReturnHttpNotAcceptable = true;
			});

			return builder;
		}

		public static IMvcBuilder AddCustomModelValidation(this IMvcBuilder builder)
		{
			builder.AddFluentValidation(c =>
			{
				c.DisableDataAnnotationsValidation = true;
				c.ImplicitlyValidateChildProperties = true;
				c.LocalizationEnabled = true;
				//TODO Use https://docs.microsoft.com/en-us/aspnet/core/fundamentals/localization?view=aspnetcore-6.0
				c.ValidatorOptions.LanguageManager.Culture = new System.Globalization.CultureInfo("ru");
			}).ConfigureApiBehaviorOptions(action =>
			{
				action.SuppressMapClientErrors = true;
				// TODO Not implemented
				action.ClientErrorMapping[400].Title = "Плохой запрос";
				action.ClientErrorMapping[400].Link = "https://wiki.developer.mozilla.org/ru/docs/Web/HTTP/Status/400";
				action.ClientErrorMapping[401].Title = "Неавторизован";
				action.ClientErrorMapping[401].Link = "https://wiki.developer.mozilla.org/ru/docs/Web/HTTP/Status/401";
				action.ClientErrorMapping[403].Title = "Запрещено";
				action.ClientErrorMapping[403].Link = "https://wiki.developer.mozilla.org/ru/docs/Web/HTTP/Status/403";
				action.ClientErrorMapping[404].Title = "Не найдено";
				action.ClientErrorMapping[404].Link = "https://wiki.developer.mozilla.org/ru/docs/Web/HTTP/Status/404";
				action.ClientErrorMapping[406].Title = "Неприемлемо";
				action.ClientErrorMapping[406].Link = "https://wiki.developer.mozilla.org/ru/docs/Web/HTTP/Status/406";
				action.ClientErrorMapping[409].Title = "Конфликт";
				action.ClientErrorMapping[409].Link = "https://wiki.developer.mozilla.org/ru/docs/Web/HTTP/Status/409";
				action.ClientErrorMapping[415].Title = "Не поддерживаемый тип содержимого";
				action.ClientErrorMapping[415].Link = "https://wiki.developer.mozilla.org/ru/docs/Web/HTTP/Status/415";
				action.ClientErrorMapping[422].Title = "Не обрабатываемая сущность";
				action.ClientErrorMapping[422].Link = "https://wiki.developer.mozilla.org/ru/docs/Web/HTTP/Status/422";
				action.ClientErrorMapping[500].Title = "Внутренняя ошибка сервера.";
				action.ClientErrorMapping[500].Link = "https://wiki.developer.mozilla.org/ru/docs/Web/HTTP/Status/500";

				action.ClientErrorMapping[412] = new ClientErrorData
				{
					Title = "Предусловие не выполнено",
					Link = "https://wiki.developer.mozilla.org/ru/docs/Web/HTTP/Status/412",
				};
			});

			return builder;
		}

		public static IApplicationBuilder UseCustomExceptionHandler(this IApplicationBuilder app)
		{
			app.UseMiddleware<HttpExceptionHandlerMiddleware>();
			return app;
		}

		public static IApplicationBuilder UseCustomSwagger(this IApplicationBuilder app, Assembly assembly)
		{
			ApiSwaggerOptions swaggerOptions = app.ApplicationServices.GetService<ApiSwaggerOptions>();
			app.UseSwagger(options =>
			{
				options.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
				{
					string apiBasePath = swaggerOptions.ApiBasePath.EndsWith(@"/", StringComparison.Ordinal)
						? swaggerOptions.ApiBasePath.Substring(0, swaggerOptions.ApiBasePath.Length - 1)
						: swaggerOptions.ApiBasePath;

					swaggerDoc.Servers = new List<OpenApiServer>
					{
						new OpenApiServer
						{
							Url = $@"{httpReq.Scheme}://{httpReq.Host.Value}{apiBasePath}"
						}
					};
				});
			});
			app.UseSwaggerUI(options =>
			{
				// Set the Swagger UI browser document title.
				options.DocumentTitle = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product;

				options.DisplayOperationId();
				options.DisplayRequestDuration();

				options.EnableDeepLinking();
				options.EnableFilter();
				options.DocExpansion(DocExpansion.None);
			});
			app.UseReDoc(options =>
			{
				options.RoutePrefix = "docs";
			});

			return app;
		}
	}
}
