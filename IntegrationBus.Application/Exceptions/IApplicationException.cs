namespace IntegrationBus.Application.Exceptions
{
	public interface IApplicationException
	{
		/// <summary>
		/// Код ошибки для Wiki.
		/// </summary>
		public ErrorCode ErrorCode { get; }
	}
}
