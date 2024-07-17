using FluentValidation;
using Shared.Models;

namespace Shared.Validation;

public class ShipmentInfoValidator : AbstractValidator<ShipmentInfo>
{
    public ShipmentInfoValidator()
    {
        RuleFor(r => r.ProductCode)
            .NotEmpty().WithMessage("OrderCode darf nicht leer sein.")
            .MaximumLength(50).WithMessage("Die Länge von ProductCode darf nur maximal 50 Zeichen sein.");
         
        RuleFor(r => r.TrackingNumber)
            .NotEmpty().WithMessage("TrackingNumber darf nicht leer sein.")
            .MaximumLength(50).WithMessage("Die Länge von TrackingNumber darf nur maximal 50 Zeichen sein.");
        
        RuleFor(r => r.Carrier)
            .NotEmpty().WithMessage("Carrier darf nicht leer sein.")
            .MaximumLength(50).WithMessage("Die Länge von Carrier darf nur maximal 50 Zeichen sein.");
        
        RuleFor(r => r.Quantity)
            .NotEmpty().WithMessage("Quantity darf nicht leer sein.");
        
        RuleFor(r => r.ReturnEntryId)
            .NotEmpty().WithMessage("ReturnEntryId darf nicht leer sein.");
    }   
}