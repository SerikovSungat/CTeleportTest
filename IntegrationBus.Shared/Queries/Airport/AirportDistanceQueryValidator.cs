using FluentValidation;

namespace IntegrationBus.Shared.Queries.Airport
{
    public class AirportDistanceQueryValidator : AbstractValidator<AirportDistanceQuery> 
    {
        public AirportDistanceQueryValidator()
        {
            RuleFor(x => x.airIATAPortCodes).NotNull().WithMessage("Нужно передать список IATA кодов как параметры");
            RuleFor(x => x.airIATAPortCodes).SetValidator(new StringDistanceQueryValidator());
        }
    }

    public class StringDistanceQueryValidator : AbstractValidator<List<string>>
    {
        public StringDistanceQueryValidator()
        {
            RuleFor(x => x).NotEmpty().NotNull().WithMessage("Заполните все параметры списка IATA кодов");
            RuleFor(x => x).Must(x => x.Count == 3).WithMessage("Длина каждого IATA кода должен быть равен 3 символа");
        }
    }
}
    
