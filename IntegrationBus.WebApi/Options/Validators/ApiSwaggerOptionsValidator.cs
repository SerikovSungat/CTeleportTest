using IntegrationBus.WebApi.Options;
using Microsoft.Extensions.Options;

namespace IntegrationBus.WebApi.WebApi.Options.Validators;

public class ApiSwaggerOptionsValidator : IValidateOptions<ApiSwaggerOptions>
{
	public ValidateOptionsResult Validate(string name, ApiSwaggerOptions options)
	{
		var failures = new List<string>();

		if (string.IsNullOrWhiteSpace(options.ApiBasePath))
		{
			failures.Add($"{nameof(options.ApiBasePath)} option is not found.");
		}

		if (failures.Count > 0)
		{
			return ValidateOptionsResult.Fail(failures);
		}

		return ValidateOptionsResult.Success;
	}
}
