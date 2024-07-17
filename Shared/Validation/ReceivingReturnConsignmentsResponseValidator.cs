using FluentValidation;
using Shared.Models;

namespace Shared.Validation;

public class ReceivingReturnConsignmentsResponseValidator : AbstractValidator<ReceivingReturnConsignmentsResponse>
{
    public ReceivingReturnConsignmentsResponseValidator()
    {
        RuleFor(r => r.consignmentCode)
            .NotEmpty().WithMessage("consignmentCode darf nicht leer sein.")
            .MaximumLength(50).WithMessage("Die Länge von consignmentCode darf nur maximal 50 Zeichen sein.");
        
        RuleFor(r => r.quantity)
            .NotEmpty().WithMessage("quantity darf nicht leer sein.");
        
        RuleFor(r => r.carrier)
            .NotEmpty().WithMessage("carrier darf nicht leer sein.")
            .MaximumLength(50).WithMessage("Die Länge von carrier darf nur maximal 50 Zeichen sein.");

        RuleFor(r => r.carrierCode)
                    .MaximumLength(50).WithMessage("Die Länge von carrierCode darf nur maximal 50 Zeichen sein.");


        RuleForEach(x => x.packages).SetValidator(new ReceivingReturnPackagesResponseValidator());

    }
}