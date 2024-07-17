using FluentValidation;
using Shared.Models;

namespace Shared.Validation;

public class ReceivingReturnRequestValidator : AbstractValidator<ReceivingReturnRequest>
{
    public ReceivingReturnRequestValidator()
    {
        RuleFor(r => r.aldiReturnCode)
            .NotEmpty().WithMessage("aldiReturnCode darf nicht leer sein.")
            .MaximumLength(50).WithMessage("Die Länge von aldiReturnCode darf nur maximal 50 Zeichen sein.");

        RuleFor(r => r.initiationDate)
            .NotEmpty().WithMessage("initiationDate darf nicht leer sein.")
            .MaximumLength(100).WithMessage("Die Länge von initiationDate darf nur maximal 100 Zeichen sein.");
        
        RuleFor(r => r.orderCode)
            .NotEmpty().WithMessage("orderCode darf nicht leer sein.")
            .MaximumLength(100).WithMessage("Die Länge von orderCode darf nur maximal 100 Zeichen sein.");

        RuleFor(x => x.customerInfo).SetValidator(new ReceivingReturnCustomerInfoRequestValidator()); 
        RuleForEach(x => x.entries).SetValidator(new ReceivingReturnEntriesRequestValidator());
    }
}