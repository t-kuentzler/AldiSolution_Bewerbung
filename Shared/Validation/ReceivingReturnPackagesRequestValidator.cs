using FluentValidation;
using Shared.Models;

namespace Shared.Validation;

public class ReceivingReturnPackagesRequestValidator : AbstractValidator<ReceivingReturnPackagesRequest>
{
    public ReceivingReturnPackagesRequestValidator()
    {
        RuleFor(r => r.status)
            .NotEmpty().WithMessage("status darf nicht leer sein.")
            .MaximumLength(50).WithMessage("Die Länge von status darf nur maximal 50 Zeichen sein.");

        RuleFor(r => r.trackingId)
            .NotEmpty().WithMessage("trackingId darf nicht leer sein.")
            .MaximumLength(50).WithMessage("Die Länge von trackingId darf nur maximal 50 Zeichen sein.");

        RuleFor(r => r.trackingLink)
            .NotEmpty().WithMessage("trackingLink darf nicht leer sein.")
            .MaximumLength(100).WithMessage("Die Länge von trackingLink darf nur maximal 100 Zeichen sein.");

        RuleFor(r => r.vendorPackageCode)
            .NotEmpty().WithMessage("vendorPackageCode darf nicht leer sein.")
            .MaximumLength(100).WithMessage("Die Länge von vendorPackageCode darf nur maximal 100 Zeichen sein.");
    }
}