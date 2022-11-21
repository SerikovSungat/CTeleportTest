using IntegrationBus.Application.Abstract.Services;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationBus.Services
{
	public class AppProblemDetailsFactory : IProblemDetailsFactory
	{
		private readonly string correlationIdKey = "correlationId";
		private readonly string traceIdKey = "traceId";

		public ProblemDetails CreateProblemDetails(HttpContext httpContext, int? statusCode = null, string? title = null, string? type = null, string? detail = null, string? instance = null)
		{
			if (httpContext is null)
			{
				throw new ArgumentNullException(nameof(httpContext));
			}

			statusCode ??= StatusCodes.Status500InternalServerError;

			var problemDetails = new ProblemDetails()
			{
				Status = statusCode,
				Title = title,
				Type = type,
				Detail = detail,
				Instance = instance
			};

			// Enrich ProblemDetails with context
			IRuntimeContextAccessor runtimeContext = httpContext.RequestServices.GetRequiredService<IRuntimeContextAccessor>();
			Guid correlationId = runtimeContext.GetCorrelationId();
			string traceId = runtimeContext.GetTraceId();

			if (correlationId != Guid.Empty)
			{
				problemDetails.Extensions.Add(this.correlationIdKey, correlationId);
			}

			if (!string.IsNullOrWhiteSpace(traceId))
			{
				problemDetails.Extensions.Add(this.traceIdKey, traceId);
			}

			return problemDetails;
		}

		public ValidationProblemDetails CreateValidationProblemDetails(HttpContext httpContext, ValidationResult validationResult, int? statusCode = null, string? title = null, string? type = null, string? detail = null, string? instance = null)
		{
			if (httpContext is null)
			{
				throw new ArgumentNullException(nameof(httpContext));
			}

			if (validationResult is null)
			{
				throw new ArgumentNullException(nameof(validationResult));
			}

			IActionContextAccessor actionContext = httpContext.RequestServices.GetRequiredService<IActionContextAccessor>();

			if (!validationResult.IsValid)
			{
				string prefix = string.Empty;
				foreach (ValidationFailure error in validationResult.Errors)
				{
					string key = string.IsNullOrEmpty(prefix)
						? error.PropertyName
						: string.IsNullOrEmpty(error.PropertyName)
							? prefix
							: prefix + "." + error.PropertyName;
					actionContext.ActionContext.ModelState.AddModelError(key, error.ErrorMessage);
				}
			}

			ValidationProblemDetails validationProblemDetails = this.CreateValidationProblemDetails(
				httpContext,
				actionContext.ActionContext.ModelState,
				statusCode,
				title,
				type,
				detail,
				instance
			);

			return validationProblemDetails;
		}

		public ValidationProblemDetails CreateValidationProblemDetails(HttpContext httpContext, ModelStateDictionary modelStateDictionary, int? statusCode = null, string? title = null, string? type = null, string? detail = null, string? instance = null)
		{
			if (httpContext is null)
			{
				throw new ArgumentNullException(nameof(httpContext));
			}

			if (modelStateDictionary is null)
			{
				throw new ArgumentNullException(nameof(modelStateDictionary));
			}

			statusCode ??= StatusCodes.Status400BadRequest;
			title ??= "An error occurred while validating input parameters.";
			detail ??= "See 'errors' property for more details";
			instance ??= httpContext.Request.Path;
			type ??= "https://tools.ietf.org/html/rfc7231#section-6.5.1";

			var problemDetails = new ValidationProblemDetails(modelStateDictionary)
			{
				Title = title,
				Status = statusCode,
				Detail = detail,
				Instance = instance,
				Type = type
			};

			// Enrich ProblemDetails with context
			IRuntimeContextAccessor runtimeContext = httpContext.RequestServices.GetRequiredService<IRuntimeContextAccessor>();
			Guid correlationId = runtimeContext.GetCorrelationId();
			string traceId = runtimeContext.GetTraceId();

			if (correlationId != Guid.Empty)
			{
				problemDetails.Extensions.Add(this.correlationIdKey, correlationId);
			}

			if (!string.IsNullOrWhiteSpace(traceId))
			{
				problemDetails.Extensions.Add(this.traceIdKey, traceId);
			}

			return problemDetails;
		}
	}
}
