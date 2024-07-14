using FluentValidation;
using Shared.Entities;


namespace Shared.Validation
{
    public class OrderValidator : AbstractValidator<Order>
    {
        public OrderValidator()
        {
            RuleFor(order => order.Code)
                .NotEmpty().WithMessage("Der Code darf nicht leer sein.")
                .Length(1, 100).WithMessage("Die Länge des Codes muss zwischen 1 und 100 Zeichen liegen.");

            RuleFor(order => order.Status)
                .NotEmpty().WithMessage("Der Status darf nicht leer sein.")
                .Length(1, 30).WithMessage("Die Länge des Status muss zwischen 1 und 30 Zeichen liegen.");

            RuleFor(order => order.Created)
                .NotEmpty().WithMessage("Das Erstellungsdatum Created darf nicht leer sein.");

            RuleFor(order => order.Modified)
                .NotEmpty().WithMessage("Das Änderungsdatum Modified darf nicht leer sein.");

            RuleFor(order => order.AldiCustomerNumber)
                .NotEmpty().WithMessage("Die AldiCustomerNumber darf nicht leer sein.")
                .Length(1, 50).WithMessage("Die Länge der AldiCustomerNumber muss zwischen 1 und 50 Zeichen liegen.");

            RuleFor(order => order.EmailAddress)
                .Length(0, 100).WithMessage("Die Länge der EmailAddress darf maximal 100 Zeichen betragen.");

            RuleFor(order => order.Phone)
                .Length(0, 30).WithMessage("Die Länge von Phone darf maximal 30 Zeichen betragen.");

            RuleFor(order => order.Language)
                .NotEmpty().WithMessage("Die Language darf nicht leer sein.")
                .Length(2).WithMessage("Die Länge der Language muss genau 2 Zeichen betragen.");

            RuleFor(order => order.OrderDeliveryArea)
                .NotEmpty().WithMessage("Die OrderDeliveryArea darf nicht leer sein.")
                .Length(1).WithMessage("Die Länge der OrderDeliveryArea muss genau 1 Zeichen betragen.");
            
            RuleForEach(x => x.Entries).SetValidator(new OrderEntryValidator());

        }
    }
}
