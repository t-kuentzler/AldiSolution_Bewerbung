using FluentValidation;
using Shared.Entities;

namespace Shared.Validation;

public class ShippingAddressValidator : AbstractValidator<ShippingAddress>
{
    public ShippingAddressValidator()
    {
        RuleFor(shippingAddress => shippingAddress.Type)
            .NotEmpty().WithMessage("Type darf nicht leer sein.")
            .Length(1, 30).WithMessage("Die Länge des Type muss zwischen 1 und 30 Zeichen liegen.");
        
        RuleFor(shippingAddress => shippingAddress.SalutationCode)
            .NotEmpty().WithMessage("SalutationCode darf nicht leer sein.")
            .Length(1, 30).WithMessage("Die Länge des SalutationCode muss zwischen 1 und 30 Zeichen liegen.");
        
        RuleFor(shippingAddress => shippingAddress.FirstName)
            .NotEmpty().WithMessage("FirstName darf nicht leer sein.")
            .Length(1, 150).WithMessage("Die Länge des FirstName muss zwischen 1 und 150 Zeichen liegen.");
        
        RuleFor(shippingAddress => shippingAddress.LastName)
            .NotEmpty().WithMessage("LastName darf nicht leer sein.")
            .Length(1, 150).WithMessage("Die Länge des LastName muss zwischen 1 und 150 Zeichen liegen.");
        
        RuleFor(shippingAddress => shippingAddress.StreetName)
            .NotEmpty().WithMessage("StreetName darf nicht leer sein.")
            .Length(1, 100).WithMessage("Die Länge des StreetName muss zwischen 1 und 100 Zeichen liegen.");
        
        RuleFor(shippingAddress => shippingAddress.StreetNumber)
            .NotEmpty().WithMessage("StreetNumber darf nicht leer sein.")
            .Length(1, 100).WithMessage("Die Länge der StreetNumber muss zwischen 1 und 100 Zeichen liegen.");
        
        RuleFor(shippingAddress => shippingAddress.Remarks)
            .MaximumLength(200).WithMessage("Die Länge des Remarks darf maximal 200 Zeichen haben.");
        
        RuleFor(shippingAddress => shippingAddress.PostalCode)
            .NotEmpty().WithMessage("PostalCode darf nicht leer sein.")
            .Length(1, 10).WithMessage("Die Länge der PostalCode muss zwischen 1 und 10 Zeichen liegen.");
        
        RuleFor(shippingAddress => shippingAddress.Town)
            .NotEmpty().WithMessage("Town darf nicht leer sein.")
            .Length(1, 50).WithMessage("Die Länge des Town muss zwischen 1 und 50 Zeichen liegen.");
        
        RuleFor(shippingAddress => shippingAddress.PackstationNumber)
            .NotEmpty().WithMessage("PackstationNumber darf nicht leer sein.")
            .Length(1, 30).WithMessage("Die Länge der PackstationNumber muss zwischen 1 und 30 Zeichen liegen.");
        
        RuleFor(shippingAddress => shippingAddress.PostNumber)
            .MaximumLength(10).WithMessage("Die Länge der PostNumber darf maximal 10 Zeichen haben.");
        
         RuleFor(shippingAddress => shippingAddress.PostOfficeNumber)
             .MaximumLength(10).WithMessage("Die Länge der PostOfficeNumber darf maximal 10 Zeichen haben.");
        
         RuleFor(shippingAddress => shippingAddress.CountryIsoCode)
             .NotEmpty().WithMessage("CountryIsoCode darf nicht leer sein.")
             .Length(1, 3).WithMessage("Die Länge des CountryIsoCode muss zwischen 1 und 3 Zeichen liegen.");
        
    }
}