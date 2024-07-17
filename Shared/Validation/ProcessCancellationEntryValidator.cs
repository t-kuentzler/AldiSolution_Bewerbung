using FluentValidation;
using Shared.Models;

namespace Shared.Validation;

public class ProcessCancellationEntryValidator : AbstractValidator<ProcessCancellationEntry>
{
    public ProcessCancellationEntryValidator()
    {
        RuleFor(pce => pce.Order)
            .NotNull().WithMessage("Order darf nicht null sein.");

        RuleFor(model => model.OrderEntry)
            .NotNull().WithMessage("OrderEntry darf nicht null sein.");

        RuleFor(model => model.OrderCancellationEntry)
            .NotNull().WithMessage("OrderCancellationEntry darf nicht null sein.");

        RuleFor(model => model.OrderCancellationEntry.cancelQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Die Stornierungsmenge muss positiv sein.");
    }
}