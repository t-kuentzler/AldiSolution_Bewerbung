using FluentValidation;
using Shared.Models;

namespace Shared.Validation;

public class ReceivingReturnConsignmentsRequestValidator : AbstractValidator<ReceivingReturnConsignmentsRequest>
{
    public ReceivingReturnConsignmentsRequestValidator()
    {
        RuleFor(r => r.carrier)
            .NotEmpty().WithMessage("carrier darf nicht leer sein.")
            .MaximumLength(50).WithMessage("Die Länge von carrier darf nur maximal 50 Zeichen sein.");

        RuleFor(r => r.quantity)
            .NotEmpty().WithMessage("quantity darf nicht leer sein.");
        
        RuleForEach(x => x.packages).SetValidator(new ReceivingReturnPackagesRequestValidator());


    }
}