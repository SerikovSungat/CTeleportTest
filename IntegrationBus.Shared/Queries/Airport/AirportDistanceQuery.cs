using IntegrationBus.Shared.Dtos.Airport;
using MediatR;

namespace IntegrationBus.Shared.Queries.Airport
{
	public class AirportDistanceQuery : IRequest<AirportDto>
	{
		public List<string> airIATAPortCodes { get; set; }
    }
}
