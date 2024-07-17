using FluentValidation;
using Shared.Models;

namespace Shared.Validation;

public class ReceivingReturnEntriesResponseValidator : AbstractValidator<ReceivingReturnEntriesResponse>
{
    public ReceivingReturnEntriesResponseValidator()
    {
        RuleFor(r => r.reason)
            .NotEmpty().WithMessage("reason darf nicht leer sein.")
            .MaximumLength(50).WithMessage("Die Länge von reason darf nur maximal 50 Zeichen sein.");

        RuleFor(r => r.notes)
            .MaximumLength(500).WithMessage("Die Länge von notes darf nur maximal 500 Zeichen sein.");

        RuleFor(r => r.orderEntryNumber)
            .NotEmpty().WithMessage("orderEntryNumber darf nicht leer sein.");

        RuleFor(r => r.quantity)
            .NotEmpty().WithMessage("quantity darf nicht leer sein.");

        RuleFor(r => r.entryCode)
            .NotEmpty().WithMessage("entryCode darf nicht leer sein.")
            .MaximumLength(100).WithMessage("Die Länge von entryCode darf nur maximal 100 Zeichen sein.");

        RuleForEach(x => x.consignments).SetValidator(new ReceivingReturnConsignmentsResponseValidator());

    }   
}