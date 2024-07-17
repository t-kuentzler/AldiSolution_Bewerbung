using FluentValidation;
using Shared.Models;

namespace Shared.Validation;

public class ReceivingReturnAddressRequestValidator : AbstractValidator<ReceivingReturnAddressRequest>
{
    public ReceivingReturnAddressRequestValidator()
    {
        RuleFor(a => a.countryIsoCode)
            .NotEmpty().WithMessage("countryIsoCode darf nicht leer sein.")
            .MaximumLength(3).WithMessage("Die Länge von countryIsoCode darf nur maximal 3 Zeichen sein.");

        RuleFor(a => a.firstName)
            .MaximumLength(100).WithMessage("Die Länge von firstName darf nur maximal 100 Zeichen sein.");

        RuleFor(a => a.lastName)
            .MaximumLength(100).WithMessage("Die Länge von lastName darf nur maximal 100 Zeichen sein.");

        RuleFor(a => a.streetName)
            .MaximumLength(100).WithMessage("Die Länge von streetName darf nur maximal 100 Zeichen sein.");

        RuleFor(a => a.streetNumber)
            .MaximumLength(20).WithMessage("Die Länge von streetNumber darf nur maximal 20 Zeichen sein.");

        RuleFor(a => a.postalCode)
            .MaximumLength(10).WithMessage("Die Länge von postalCode darf nur maximal 10 Zeichen sein.");

        RuleFor(a => a.town)
            .MaximumLength(100).WithMessage("Die Länge von town darf nur maximal 100 Zeichen sein.");

        RuleFor(a => a.type)
            .NotEmpty().WithMessage("type darf nicht leer sein.")
            .MaximumLength(50).WithMessage("Die Länge von type darf nur maximal 50 Zeichen sein.");
    }
}