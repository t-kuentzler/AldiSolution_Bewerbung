using FluentValidation;
using Shared.Entities;

namespace Shared.Validation;

public class ReturnValidator : AbstractValidator<Return>
{
    public ReturnValidator() 
    {
        RuleFor(r => r.OrderCode)
            .NotEmpty().WithMessage("OrderCode darf nicht leer sein.")
            .MaximumLength(100).WithMessage("Die Länge von OrderCodes darf nur maximal 100 Zeichen sein.");

        RuleFor(r => r.InitiationDate)
            .NotEmpty().WithMessage("InitiationDate darf nicht leer sein.");

        RuleFor(r => r.Rma)
            .MaximumLength(50).WithMessage("Die Länge von Rma darf nur maximal 50 Zeichen sein.");
        
        RuleFor(r => r.Status)
            .NotEmpty().WithMessage("Status darf nicht leer sein.")
            .MaximumLength(50).WithMessage("Die Länge von Status darf nur maximal 50 Zeichen sein.");
        
        RuleFor(r => r)
            .NotNull().WithMessage("Return-Objekt darf nicht null sein.");

        RuleFor(r => r.ReturnEntries)
            .NotEmpty().WithMessage("Return-Objekt muss mindestens einen ReturnEntry enthalten.");
        
        RuleFor(x => x.CustomerInfo).SetValidator(new CustomerInfoValidator()); 
        RuleForEach(x => x.ReturnEntries).SetValidator(new ReturnEntryValidator());
    }
}