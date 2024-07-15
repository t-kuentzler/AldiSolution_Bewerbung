using FluentValidation;
using Shared.Entities;

namespace Shared.Validation;

public class ReturnConsignmentValidator : AbstractValidator<ReturnConsignment?>
{
    public ReturnConsignmentValidator()
    {
        RuleFor(r => r.ConsignmentCode)
            .NotEmpty().WithMessage("ConsignmentCode darf nicht leer sein.")
            .MaximumLength(50).WithMessage("Die Länge von ConsignmentCode darf nur maximal 50 Zeichen sein.");
        
        RuleFor(r => r.Quantity)
            .NotEmpty().WithMessage("Quantity darf nicht leer sein.");
        
        RuleFor(r => r.Carrier)
            .MaximumLength(50).WithMessage("Die Länge von Carrier darf nur maximal 50 Zeichen sein.");
       
        RuleFor(r => r.Status)
            .NotEmpty().WithMessage("TrackingId darf nicht leer sein.")
            .MaximumLength(50).WithMessage("Die Länge von TrackingId darf nur maximal 50 Zeichen sein.");
        
        RuleForEach(x => x.Packages).SetValidator(new ReturnPackageValidator());

    }
}