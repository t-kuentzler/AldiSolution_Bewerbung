using FluentValidation;
using Shared.Entities;

namespace Shared.Validation
{
    public class OrderEntryValidator : AbstractValidator<OrderEntry>
    {
        public OrderEntryValidator()
        {
            RuleFor(entry => entry.EntryNumber)
                .NotEmpty().WithMessage("Die EntryNumber darf nicht leer sein.");

            RuleFor(entry => entry.VendorProductCode)
                .NotEmpty().WithMessage("Der VendorProductCode darf nicht leer sein.");

            RuleFor(entry => entry.AldiProductCode)
                .NotEmpty().WithMessage("Der AldiProductCode darf nicht leer sein.");
            
            RuleFor(entry => entry.ProductName)
                .NotEmpty().WithMessage("Der ProductName darf nicht leer sein.")
                .Length(1, 100).WithMessage("Die Länge des Produktnamens muss zwischen 1 und 100 Zeichen liegen.");

            RuleFor(entry => entry.Quantity)
                .NotEmpty().WithMessage("Die Quantity darf nicht leer sein.");

            RuleFor(entry => entry.CarrierCode)
                .Length(0, 50).WithMessage("Die Länge des CarrierCode darf maximal 50 Zeichen betragen.");

            RuleFor(entry => entry.AldiSuedProductCode)
                .Length(0, 50).WithMessage("Die Länge des AldiSuedProductCode darf maximal 50 Zeichen betragen.");
            
            RuleFor(x => x.DeliveryAddress)
                .SetValidator(new DeliveryAddressValidator())
                .When(x => x.DeliveryAddress != null);


        }
    }
}
