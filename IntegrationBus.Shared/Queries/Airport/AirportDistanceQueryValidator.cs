using FluentValidation;

namespace IntegrationBus.Shared.Queries.Airport
{
	public class AirportDistanceQueryValidator : AbstractValidator<AirportDistanceQuery>
	{
		public AirportDistanceQueryValidator()
		{
            RuleFor(x => x.airIATAPortCodes).NotNull();
        }
    }
}
