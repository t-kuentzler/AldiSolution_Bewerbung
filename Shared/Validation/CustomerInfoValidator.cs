using FluentValidation;
using Shared.Entities;

namespace Shared.Validation;

public class CustomerInfoValidator : AbstractValidator<CustomerInfo>
{
    public CustomerInfoValidator()
    {
        RuleFor(r => r.EmailAddress)
            .MaximumLength(50).WithMessage("Die Länge von EmailAddress darf nur maximal 50 Zeichen sein.");
        
        RuleFor(r => r.PhoneNumber)
            .MaximumLength(50).WithMessage("Die Länge von PhoneNumber darf nur maximal 50 Zeichen sein.");
        
        RuleFor(x => x.Address)
            .SetValidator(new AddressValidator())
            .When(x => x.Address != null);
    }
}