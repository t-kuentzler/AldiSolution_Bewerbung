using AldiOrderManagement.Validation;
using FluentValidation;
using Shared.Models;

namespace Shared.Validation;

public class ReceivingReturnCustomerInfoResponseValidator : AbstractValidator<ReceivingReturnCustomerInfoResponse>
{
    public ReceivingReturnCustomerInfoResponseValidator()
    {
        RuleFor(r => r.emailAddress)
            .NotEmpty().WithMessage("emailAddress darf nicht leer sein.")
            .MaximumLength(50).WithMessage("Die Länge von emailAddress darf nur maximal 50 Zeichen sein.");
    
        RuleFor(r => r.phoneNumber)
            .MaximumLength(50).WithMessage("Die Länge von phoneNumber darf nur maximal 50 Zeichen sein.");
        
        RuleFor(x => x.address).SetValidator(new ReceivingReturnAddressResponseValidator()); 


    }
}