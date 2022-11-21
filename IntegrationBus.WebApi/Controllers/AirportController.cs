using IntegrationBus.Shared.Dtos.Airport;
using IntegrationBus.Shared.Queries.Airport;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace IntegrationBus.WebApi.Controllers
{
    /// <summary>
    /// API контроллер сервисов связанные с аэропортами.
    /// </summary>
    [Route("/airport")]
	[ApiController]
	[SwaggerTag("Сервисы связанные с аэропортами.")]
	public class AirportController : ControllerBase
	{
		private readonly IMediator mediator;

		/// <summary>
		/// Конструктор контроллера.
		/// </summary>
		/// <param name="mediator"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public AirportController(IMediator mediator)
		{
			this.mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
		}

        /// <summary>
        /// Получение расстояния между аэропортами.
        /// </summary>
        /// <param name="query">Запрос для получения текста.</param>
        /// <param name="cancellationToken">Маркер отмены, используемый для отмены HTTP-запроса.</param>
        /// <returns></returns>
      [HttpPost("measure-distance")]
	  [SwaggerResponse(StatusCodes.Status200OK, "Расчет расстояния между аэропортами.", typeof(AirportDto))]
	  public async Task<IActionResult> GetMeasureBetweenAirports([FromBody] AirportDistanceQuery query, CancellationToken cancellationToken)
	  {
		var result = await this.mediator.Send(query, cancellationToken);
		return this.Ok(result);
	  }
    }
}
