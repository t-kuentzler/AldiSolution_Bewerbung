using FluentValidation;
using Shared.Models;

namespace Shared.Validation;

public class CancelOrderEntryValidator : AbstractValidator<CancelOrderEntryModel>
{
    public CancelOrderEntryValidator()
    {
        RuleFor(x => x.CancelQuantity)
            .GreaterThan(0)
            .When(x => x.IsCancelled)
            .WithMessage("Bitte geben Sie eine gültige Menge für die stornierten Bestellpositionen ein.");
    }

}