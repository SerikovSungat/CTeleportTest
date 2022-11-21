namespace IntegrationBus.Application.Constants
{
	public static class Logs
	{
		public const string CorrelationId = nameof(CorrelationId);
		public const string ClientIp = nameof(ClientIp);
		public const string LogType = nameof(LogType);

		// Request fields
		public const string ClientRequestId = nameof(ClientRequestId);
		public const string RequestHeaders = nameof(RequestHeaders);
		public const string RequestBody = nameof(RequestBody);
		public const string RequestProtocol = nameof(RequestProtocol);
		public const string RequestScheme = nameof(RequestScheme);
		public const string RequestHost = nameof(RequestHost);
		public const string RequestMethod = nameof(RequestMethod);
		public const string RequestPath = nameof(RequestPath);
		public const string RequestQuery = nameof(RequestQuery);
		public const string RequestPathAndQuery = nameof(RequestPathAndQuery);

		// Response fields
		public const string ResponseHeaders = nameof(ResponseHeaders);
		public const string ResponseBody = nameof(ResponseBody);
		public const string ElapsedMilliseconds = nameof(ElapsedMilliseconds);
		public const string StatusCode = nameof(StatusCode);
		public const string RequestAborted = nameof(RequestAborted);
	}
}
