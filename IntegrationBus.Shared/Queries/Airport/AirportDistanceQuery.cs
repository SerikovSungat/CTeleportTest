using IntegrationBus.Shared.Dtos.Airport;
using MediatR;

namespace IntegrationBus.Shared.Queries.Airport
{
	public class AirportDistanceQuery : IRequest<AirportDto>
	{
		public string[] airIATAPortCodes { get; set; }
    }
}
