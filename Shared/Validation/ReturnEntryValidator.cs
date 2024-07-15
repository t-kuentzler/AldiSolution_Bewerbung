using FluentValidation;
using Shared.Entities;

namespace Shared.Validation;

public class ReturnEntryValidator : AbstractValidator<ReturnEntry>
{
    public ReturnEntryValidator()
    {
        RuleFor(r => r.Reason)
            .MaximumLength(50).WithMessage("Die Länge von Reason darf nur maximal 50 Zeichen sein.");

        RuleFor(r => r.Notes)
            .MaximumLength(500).WithMessage("Die Länge von Notes darf nur maximal 500 Zeichen sein.");

        RuleFor(r => r.Quantity)
            .GreaterThan(0).WithMessage("Quantity muss größer als 0 sein.")
            .NotEmpty().WithMessage("Quantity darf nicht leer sein.");

        RuleFor(r => r.OrderEntryNumber)
            .NotEmpty().WithMessage("OrderEntryNumber darf nicht leer sein.");

        RuleFor(r => r.EntryCode)
            .MaximumLength(100).WithMessage("Die Länge von EntryCode darf nur maximal 100 Zeichen sein.");
        
        RuleFor(r => r.Status)
            .NotEmpty().WithMessage("Status darf nicht leer sein.")
            .MaximumLength(50).WithMessage("Die Länge von Status darf nur maximal 50 Zeichen sein.");
        
        RuleFor(r => r.CarrierCode)
            .MaximumLength(100).WithMessage("Die Länge von CarrierCode darf nur maximal 100 Zeichen sein.");
        
        RuleForEach(x => x.ReturnConsignments)
            .SetValidator(new ReturnConsignmentValidator())
            .When(x => x.ReturnConsignments != null);
    }
}