using FluentValidation;
using Shared.Entities;

namespace Shared.Validation;

public class ConsignmentEntryValidator : AbstractValidator<ConsignmentEntry>
{
    public ConsignmentEntryValidator()
    {
        RuleFor(consignmentEntry => consignmentEntry.OrderEntryNumber)
            .NotNull().WithMessage("OrderEntryNumber darf nicht leer sein.");
        
        RuleFor(consignmentEntry => consignmentEntry.Quantity)
            .NotNull().WithMessage("Quantity darf nicht leer sein.");
        
        RuleFor(consignmentEntry => consignmentEntry.CancelledOrReturnedQuantity)
            .NotNull().WithMessage("CancelledOrReturnedQuantity darf nicht leer sein.");
    }
}