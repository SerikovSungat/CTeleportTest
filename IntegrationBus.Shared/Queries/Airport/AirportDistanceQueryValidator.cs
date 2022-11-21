using FluentValidation;

namespace IntegrationBus.Shared.Queries.Airport
{
    public class AirportDistanceQueryValidator : AbstractValidator<AirportDistanceQuery> 
    {
        public AirportDistanceQueryValidator()
        {
                 RuleForEach(x => x.airIATAPortCodes)
                .NotNull().WithMessage("Объязательно заполните параметры")
                .Must(x => x.Length == 3).WithMessage("Длина каждого IATA кода должен быть равен 3 символа");

                RuleFor(x=>x.airIATAPortCodes)
                .Must(x => x.Count == 2).WithMessage("Введите максимум 2 IATA кода в параметрах");
        }
    }
}
    
