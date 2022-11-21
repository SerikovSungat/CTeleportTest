using FluentValidation;

namespace IntegrationBus.Shared.Queries.Airport
{
    public class AirportDistanceQueryValidator : AbstractValidator<AirportDistanceQuery> 
    {
        public AirportDistanceQueryValidator()
        {
            RuleFor(x => x.airIATAPortCodes).NotNull().Must(x => x.Length < 2 && x.Length > 2);
            RuleFor(x => x.airIATAPortCodes).SetValidator(new StringDistanceQueryValidator());
        }
    }

    public class StringDistanceQueryValidator : AbstractValidator<string[]>
    {
        public StringDistanceQueryValidator()
        {
            RuleFor(x => x).NotEmpty().NotNull().Must(x => x.Length < 3 && x.Length > 3);
        }
    }
}
    
