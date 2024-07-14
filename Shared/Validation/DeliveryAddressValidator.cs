using FluentValidation;
using Shared.Entities;

namespace Shared.Validation
{
    public class DeliveryAddressValidator : AbstractValidator<DeliveryAddress>
    {
        public DeliveryAddressValidator()
        {
            RuleFor(address => address.Type)
                .NotEmpty().WithMessage("Der Type darf nicht leer sein.")
                .Length(1, 30).WithMessage("Die Länge des Type muss zwischen 1 und 30 Zeichen liegen.");

            RuleFor(address => address.SalutationCode)
                .NotEmpty().WithMessage("Der SalutationCode darf nicht leer sein.")
                .Length(1, 30).WithMessage("Die Länge des SalutationCode muss zwischen 1 und 30 Zeichen liegen.");

            RuleFor(address => address.FirstName)
                .NotEmpty().WithMessage("Der FirstName darf nicht leer sein.")
                .Length(1, 150).WithMessage("Die Länge des FirstName muss zwischen 1 und 150 Zeichen liegen.");

            RuleFor(address => address.LastName)
                .NotEmpty().WithMessage("Der LastName darf nicht leer sein.")
                .Length(1, 150).WithMessage("Die Länge des LastName muss zwischen 1 und 150 Zeichen liegen.");

            RuleFor(address => address.StreetName)
                .MaximumLength(100).WithMessage("Die Länge von streetName darf nur maximal 100 Zeichen sein.");

            RuleFor(address => address.StreetNumber)
                .MaximumLength(100).WithMessage("Die Länge von streetNumber darf nur maximal 100 Zeichen sein.");

            RuleFor(address => address.PostalCode)
                .NotEmpty().WithMessage("Der PostalCode darf nicht leer sein.")
                .Length(1, 10).WithMessage("Die Länge des PostalCode muss zwischen 1 und 10 Zeichen liegen.");

            RuleFor(address => address.Town)
                .NotEmpty().WithMessage("Die Town darf nicht leer sein.")
                .Length(1, 50).WithMessage("Die Länge der Town muss zwischen 1 und 50 Zeichen liegen.");

            RuleFor(address => address.CountryIsoCode)
                .NotEmpty().WithMessage("Der CountryIsoCode darf nicht leer sein.")
                .Length(1, 3).WithMessage("Die Länge des CountryIsoCode muss zwischen 1 und 3 Zeichen liegen.");

            RuleFor(address => address.Remarks)
                .Length(0, 200).WithMessage("Die Länge der Remarks darf maximal 200 Zeichen betragen.")
                .When(address => !string.IsNullOrEmpty(address.Remarks));

            RuleFor(address => address.PackstationNumber)
                .Length(0, 30).WithMessage("Die Länge der PackstationNumber darf maximal 30 Zeichen betragen.")
                .When(address => !string.IsNullOrEmpty(address.PackstationNumber));

            RuleFor(address => address.PostNumber)
                .Length(0, 10).WithMessage("Die Länge der PostNumber darf maximal 10 Zeichen betragen.")
                .When(address => !string.IsNullOrEmpty(address.PostNumber));

            RuleFor(address => address.PostOfficeNumber)
                .Length(0, 10).WithMessage("Die Länge der PostOfficeNumber darf maximal 10 Zeichen betragen.")
                .When(address => !string.IsNullOrEmpty(address.PostOfficeNumber));
        }
    }
}
