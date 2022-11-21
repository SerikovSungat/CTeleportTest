namespace IntegrationBus.Application.Abstract.Services
{
	public interface IRuntimeContextAccessor
	{
		public Guid GetCorrelationId();
		public string GetTraceId();
	}
}
