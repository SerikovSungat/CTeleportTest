using Microsoft.Extensions.Logging;

namespace IntegrationBus.Logging.Extensions
{
	public static class LoggerExtensions
	{
		public static IDisposable BeginScopeWith(this ILogger logger, params (string key, object value)[] paramsAndValues)
		{
			return logger.BeginScope(paramsAndValues.ToDictionary(x => x.key, x => x.value));
		}
	}
}
