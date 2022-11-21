namespace IntegrationBus.WebApi.Constants
{
	public static class SwaggerResponseDescriptions
	{
		public const string Code500 = "Unexpected error has occurred, please contact support.";

		/// <summary>
		/// Allowed HTTP methods.
		/// </summary>
		public const string Code200OkAllowHttpMethods = "Разрешенные HTTP-методы.";

		/// <summary>
		/// "{ObjectName}" with the specified unique identifier.
		/// </summary>
		public const string Code200OkObject = "Ресурс с указанным уникальным идентификатором.";

		/// <summary>
		/// "{ObjectName}" was created.
		/// </summary>
		public const string Code201CreatedObject = "Ресурс был создан.";

		/// <summary>
		/// "{ObjectName}" with the specified unique identifier was deleted.
		/// </summary>
		public const string Code204OkObject = "Ресурс с указанным уникальным идентификатором удалено.";

		/// <summary>
		/// The "{ObjectName}" has not changed since the date given in the If-Modified-Since HTTP header.
		/// </summary>
		public const string Code304NotModifiedObject = "Ресурс не изменился с даты, указанной в HTTP-заголовке If-Modified-Since.";

		/// <summary>
		/// Request is invalid.
		/// </summary>
		public const string Code400BadRequest = "Неверный запрос.";

		/// <summary>
		/// Request Header is invalid.
		/// </summary>
		public const string Code400BadRequestHeader = "Запрос не содержит заголовка {{headerName}} или значение в нём неверно.";

		/// <summary>
		/// Page request parameters are invalid.
		/// </summary>
		public const string Code400BadRequestPage = "Неверный параметр запроса страницы.";

		/// <summary>
		/// "{ObjectName}" with the specified unique identifier could not be found.
		/// </summary>
		public const string Code404NotFoundObject = "Ресурс с указанным уникальным идентификатором не найден.";

		/// <summary>
		/// Page with the specified page number was not found.
		/// </summary>
		public const string Code404NotFoundPage = "Страница с указанным номером страницы не найдена.";

		/// <summary>
		/// The MIME type in the Accept HTTP header is not acceptable.
		/// </summary>
		public const string Code406NotAcceptable = "Недопустимый MIME тип в заголовке Accept HTTP.";

		/// <summary>
		/// Duplicate request.
		/// </summary>
		public const string Code409ConflictDuplicateRequest = "Повторяющийся запрос.";

		/// <summary>
		/// Existing request.
		/// </summary>
		public const string Code409ConflictConcurrencyError = "Запрос уже существует.";

		/// <summary>
		/// Duplicate request. Request parameters are different.
		/// </summary>
		public const string Code409ConflictParamError = "В кеше исполнения уже есть запрос с таким идентификатором и его параметры отличны от текущего запроса.";

		/// <summary>
		/// The MIME type in the Content-Type HTTP header is unsupported.
		/// </summary>
		public const string Code415UnsupportedMediaType = "Тип MIME в HTTP-заголовке Content-Type не поддерживается.";

		/// <summary>
		/// Internal error. 
		/// </summary>
		public const string Code500InternalServerErrorIdempotencyNotImplemented = "Обработка идемпотентности не предусмотрена для результата {{resultType}}";
	}
}
