using IntegrationBus.Shared.Dtos.Airport;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace IntegrationBus.Shared.Queries.Airport
{
	public class AirportDistanceQuery : IRequest<AirportDto>
	{
        [Required]
        public string[] airIATAPortCodes { get; set; }
    }
}
