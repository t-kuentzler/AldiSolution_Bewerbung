using FluentValidation;
using Shared.Entities;

namespace Shared.Validation;

public class AddressValidator : AbstractValidator<Address?>
{
    public AddressValidator()
    {
    
    RuleFor(a => a.Type)
        .NotEmpty().WithMessage("Type darf nicht leer sein.")
        .MaximumLength(50).WithMessage("Die Länge von Type darf nur maximal 50 Zeichen sein.");
    
    RuleFor(a => a.SalutationCode)
        .MaximumLength(10).WithMessage("Die Länge von SalutationCode darf nur maximal 10 Zeichen sein.");
    
    RuleFor(a => a.FirstName)
        .NotEmpty().WithMessage("FirstName darf nicht leer sein.")
        .MaximumLength(150).WithMessage("Die Länge von FirstName darf nur maximal 150 Zeichen sein.");
    
    RuleFor(a => a.LastName)
        .NotEmpty().WithMessage("LastName darf nicht leer sein.")
        .MaximumLength(150).WithMessage("Die Länge von LastName darf nur maximal 150 Zeichen sein.");
    
    RuleFor(a => a.StreetName)
        .MaximumLength(100).WithMessage("Die Länge von StreetName darf nur maximal 100 Zeichen sein.");
    
    RuleFor(a => a.StreetNumber)
        .MaximumLength(20).WithMessage("Die Länge von StreetNumber darf nur maximal 100 Zeichen sein.");
    
    RuleFor(a => a.Remarks)
        .MaximumLength(500).WithMessage("Die Länge von Remarks darf nur maximal 500 Zeichen sein.");
    
    RuleFor(a => a.PostalCode)
        .NotEmpty().WithMessage("PostalCode darf nicht leer sein.")
        .MaximumLength(10).WithMessage("Die Länge von PostalCode darf nur maximal 10 Zeichen sein.");
    
    RuleFor(a => a.Town)
        .NotEmpty().WithMessage("Town darf nicht leer sein.")
        .MaximumLength(100).WithMessage("Die Länge von Town darf nur maximal 100 Zeichen sein.");
    
    RuleFor(a => a.PackstationNumber)
        .MaximumLength(20).WithMessage("Die Länge von PackstationNumber darf nur maximal 20 Zeichen sein.");
    
    RuleFor(a => a.PostNumber)
        .MaximumLength(20).WithMessage("Die Länge von PostNumber darf nur maximal 20 Zeichen sein.");
    
    RuleFor(a => a.PostOfficeNumber)
        .MaximumLength(20).WithMessage("Die Länge von PostOfficeNumber darf nur maximal 20 Zeichen sein.");
    
    RuleFor(a => a.CountryIsoCode)
        .NotEmpty().WithMessage("CountryIsoCode darf nicht leer sein.")
        .MaximumLength(3).WithMessage("Die Länge von CountryIsoCode darf nur maximal 3 Zeichen sein.");
    }
}