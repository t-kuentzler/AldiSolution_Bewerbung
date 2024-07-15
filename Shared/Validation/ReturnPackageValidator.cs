using FluentValidation;
using Shared.Entities;

namespace Shared.Validation;

public class ReturnPackageValidator : AbstractValidator<ReturnPackage>
{
    public ReturnPackageValidator()
    {
        RuleFor(r => r.Status)
            .NotEmpty().WithMessage("VendorPackageCode darf nicht leer sein.")
            .MaximumLength(100).WithMessage("Die Länge von VendorPackageCode darf nur maximal 100 Zeichen sein.");
        
        RuleFor(r => r.Status)
            .NotEmpty().WithMessage("TrackingId darf nicht leer sein.")
            .MaximumLength(50).WithMessage("Die Länge von TrackingId darf nur maximal 50 Zeichen sein.");
        
        RuleFor(r => r.TrackingLink)
            .NotEmpty().WithMessage("TrackingLink darf nicht leer sein.")
            .MaximumLength(100).WithMessage("Die Länge von TrackingLink darf nur maximal 100 Zeichen sein.");
        
        RuleFor(r => r.Status)
            .NotEmpty().WithMessage("Status darf nicht leer sein.")
            .MaximumLength(50).WithMessage("Die Länge von Status darf nur maximal 50 Zeichen sein.");

        
    }
}