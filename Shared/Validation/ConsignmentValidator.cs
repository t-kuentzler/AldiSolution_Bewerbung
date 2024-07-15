using FluentValidation;
using Shared.Entities;

namespace Shared.Validation;

public class ConsignmentValidator : AbstractValidator<Consignment>
{
    public ConsignmentValidator()
    {
        RuleFor(consignment => consignment.VendorConsignmentCode)
            .NotEmpty().WithMessage("VendorConsignmentCode darf nicht leer sein.")
            .Length(1, 50).WithMessage("Die Länge des VendorConsignmentCode muss zwischen 1 und 50 Zeichen liegen.");
        
        RuleFor(consignment => consignment.StatusText)
            .NotEmpty().WithMessage("StatusText darf nicht leer sein.")
            .Length(1, 50).WithMessage("Die Länge des StatusText muss zwischen 1 und 50 Zeichen liegen.");
          
        RuleFor(consignment => consignment.TrackingId)
            .NotEmpty().WithMessage("TrackingId darf nicht leer sein.")
            .Length(1, 50).WithMessage("Die Länge der TrackingId muss zwischen 1 und 50 Zeichen liegen.");
        
        RuleFor(consignment => consignment.TrackingLink)
            .NotEmpty().WithMessage("TrackingLink darf nicht leer sein.")
            .Length(1, 100).WithMessage("Die Länge des TrackingLink muss zwischen 1 und 100 Zeichen liegen.");
        
         RuleFor(consignment => consignment.Carrier)
            .NotEmpty().WithMessage("Carrier darf nicht leer sein.")
            .Length(1, 50).WithMessage("Die Länge des Carrier muss zwischen 1 und 50 Zeichen liegen.");

         RuleFor(consignment => consignment.ShippingDate)
             .NotEmpty().WithMessage("ShippingDate darf nicht leer sein.");
         
         RuleFor(consignment => consignment.AldiConsignmentCode)
             .MaximumLength(50).WithMessage("Die Länge des AldiConsignmentCode darf maximal 50 Zeichen haben.");
         
         RuleFor(consignment => consignment.Status)
             .NotEmpty().WithMessage("Status darf nicht leer sein.")
             .Length(1, 50).WithMessage("Die Länge des Status muss zwischen 1 und 50 Zeichen liegen.");
         
         RuleFor(consignment => consignment.OrderCode)
             .NotEmpty().WithMessage("OrderCode darf nicht leer sein.");
         
         RuleForEach(x => x.ConsignmentEntries).SetValidator(new ConsignmentEntryValidator());

         RuleFor(consignment => consignment.ShippingAddress)
             .NotNull().WithMessage("ShippingAddress darf nicht null sein.");

         RuleFor(consignment => consignment.Order)
             .NotNull().WithMessage("Order darf nicht null sein.");

         RuleFor(consignment => consignment.ShippingAddress)
             .SetValidator(new ShippingAddressValidator());

         RuleFor(consignment => consignment.Order)
             .SetValidator(new OrderValidator());
    }
}