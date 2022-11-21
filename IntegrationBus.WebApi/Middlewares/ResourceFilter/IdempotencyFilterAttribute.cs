using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using IntegrationBus.Application.Abstract.Services;
using IntegrationBus.Application.Exceptions;
using IntegrationBus.WebApi.Constants;
using IntegrationBus.WebApi.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace IntegrationBus.WebApi.Middlewares.ResourceFilter
{
	public class IdempotencyFilterAttribute : Attribute, IAsyncResourceFilter
	{
		private readonly ILogger<IdempotencyFilterAttribute> logger;
		private readonly IHttpContextAccessor httpContextAccessor;
		private readonly IProblemDetailsFactory problemDetailsFactory;
		private readonly IdempotencyControlOptions options;

		public IdempotencyFilterAttribute(ILogger<IdempotencyFilterAttribute> logger,
			IHttpContextAccessor httpContextAccessor,
			IProblemDetailsFactory problemDetailsFactory,
			IOptions<IdempotencyControlOptions> options)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
			this.problemDetailsFactory = problemDetailsFactory ?? throw new ArgumentNullException(nameof(problemDetailsFactory));
			this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
		}

		public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
		{
			bool idempotencyFilterEnabled = this.options.IdempotencyFilterEnabled ?? false;

			//Filter is disabled.
			if (!idempotencyFilterEnabled)
			{
				await next.Invoke();
				return;
			}

			if (this.httpContextAccessor.HttpContext == null)
			{
				throw new ClientRequestIdNotFoundException("Cannot obtain client request ID: no HTTP context.");
			}

			Guid requestId = Guid.Empty;

			if (this.httpContextAccessor.HttpContext.Request.Headers.TryGetValue(this.options.ClientRequestIdHeader, out StringValues clientRequestIdValues))
			{
				if (Guid.TryParse(clientRequestIdValues.ToString(), out Guid clientRequestId))
				{
					requestId = clientRequestId;
				}
			}

			if (requestId == Guid.Empty)
			{
				string statusMessage = SwaggerResponseDescriptions.Code400BadRequestHeader.Replace("{{headerName}}", nameof(this.options.ClientRequestIdHeader));

				ProblemDetails problemDetails = this.problemDetailsFactory.CreateProblemDetails(this.httpContextAccessor.HttpContext, StatusCodes.Status400BadRequest, statusMessage);

				context.Result = new BadRequestObjectResult(problemDetails);
				return;
			}

			string method = context.HttpContext.Request.Method;
			string path = context.HttpContext.Request.Path.HasValue
				? context.HttpContext.Request.Path.Value
				: string.Empty;
			string queryString = context.HttpContext.Request.QueryString.HasValue
				? context.HttpContext.Request.QueryString.ToUriComponent()
				: string.Empty;

			using (var cts = new CancellationTokenSource(this.options.ApiRequestFilterMilliseconds))
			{
					ProblemDetails requestConcurrencyError = this.problemDetailsFactory.CreateProblemDetails(context.HttpContext,
						statusCode: StatusCodes.Status409Conflict,
						title: SwaggerResponseDescriptions.Code409ConflictConcurrencyError,
						type: null,
						detail: null,
						instance: path);

					context.Result = new BadRequestObjectResult(requestConcurrencyError);
					return;
				

				ResourceExecutedContext executedContext = await next.Invoke();

				int statusCode = context.HttpContext.Response.StatusCode;
				var headers = context.HttpContext.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToList());

				var jsonSettings = new JsonSerializerOptions()
				{
					DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
					PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
					Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
					WriteIndented = false
				};
				jsonSettings.Converters.Add(new JsonStringEnumConverter());

				if (executedContext.Result != null)
				{
					switch (executedContext.Result)
					{
						case CreatedAtRouteResult createdRequestResult:
						{
							string body = JsonSerializer.Serialize(createdRequestResult, jsonSettings);
							var routeValues = createdRequestResult.RouteValues.ToDictionary(v => v.Key, v => v.Value?.ToString());
							break;
						}
						case ObjectResult objectRequestResult:
						{
							string body = JsonSerializer.Serialize(objectRequestResult.Value, jsonSettings);
							break;
						}
						case NoContentResult noContentResult:
						{
							break;
						}
						case OkResult okResult:
						{
							break;
						}
						case StatusCodeResult statusCodeResult:
						case ActionResult actionResult:
						{
							break;
						}
						default:
						{
							string message = SwaggerResponseDescriptions.Code500InternalServerErrorIdempotencyNotImplemented.Replace("{{resultType}}", executedContext.GetType()?.ToString());
							throw new NotImplementedException(message);
						}
					}
				}
			}
		}
	}
}
