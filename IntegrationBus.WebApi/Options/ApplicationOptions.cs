using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace IntegrationBus.WebApi.Options
{
	/// <summary>
	/// Конфигурация приложения.
	/// </summary>
	public class ApplicationOptions
	{
		/// <summary>
		/// Конфигурация веб-сервера Kestrel.
		/// </summary>
		[Required]
		public KestrelServerOptions Kestrel { get; set; }

		/// <summary>
		/// Конфигурация Swagger.
		/// </summary>
		
		[Required]
		public ApiSwaggerOptions ApiSwagger { get; set; }

        /// <summary>
        /// Фильтр идемпотентности.
        /// </summary>
        [Required]
        public IdempotencyControlOptions IdempotencyControl { get; set; }
    }
}
