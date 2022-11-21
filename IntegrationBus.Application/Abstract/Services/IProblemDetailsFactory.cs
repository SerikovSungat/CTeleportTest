using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace IntegrationBus.Application.Abstract.Services
{
	public interface IProblemDetailsFactory
	{
		ProblemDetails CreateProblemDetails(HttpContext httpContext, int? statusCode = null, string? title = null, string? type = null, string? detail = null, string? instance = null);
		ValidationProblemDetails CreateValidationProblemDetails(HttpContext httpContext, ValidationResult validationResult, int? statusCode = null, string? title = null, string? type = null, string? detail = null, string? instance = null);
		ValidationProblemDetails CreateValidationProblemDetails(HttpContext httpContext, ModelStateDictionary modelStateDictionary, int? statusCode = null, string? title = null, string? type = null, string? detail = null, string? instance = null);
	}
}
