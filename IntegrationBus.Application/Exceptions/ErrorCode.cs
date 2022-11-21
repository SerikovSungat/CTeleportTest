using System.ComponentModel;

namespace IntegrationBus.Application.Exceptions
{
	/// <summary>
	/// Коды ошибок приложения.
	/// </summary>
	public enum ErrorCode
	{
		/// <summary>
		/// Неизвестный код ошибки.
		/// </summary>
		[Description("Неизвестный код ошибки.")]
		Unknown = 0
	}
}
