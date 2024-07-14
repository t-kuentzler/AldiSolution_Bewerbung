using FluentValidation;
using Shared.Entities;

namespace Shared.Validation
{
    public class AccessTokenValidator : AbstractValidator<AccessToken>
    {
        public AccessTokenValidator()
        {
            RuleFor(token => token.Token)
                .NotEmpty()
                .WithMessage("Token darf nicht leer sein.")
                .Length(1, 50)
                .WithMessage("Die Länge von Token muss zwischen 1 und 50 Zeichen liegen.");

            RuleFor(token => token.ExpiresAt)
                .NotEmpty()
                .WithMessage("ExpiresAt darf nicht leer sein.")
                .Must(BeAValidDate)
                .WithMessage("Ungültiges ExpiresAt.");
        }

        private bool BeAValidDate(DateTime date)
        {
            return date != default(DateTime);
        }
    }
}
