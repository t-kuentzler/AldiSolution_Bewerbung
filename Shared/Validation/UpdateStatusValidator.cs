using FluentValidation;
using Shared.Models;

namespace Shared.Validation
{
    public class UpdateStatusValidator : AbstractValidator<UpdateStatus>
    {
        public UpdateStatusValidator()
        {
            RuleFor(order => order.Code)
                .NotEmpty().WithMessage("Der Code darf nicht leer sein.")
                .Length(1, 100).WithMessage("Die Länge des Codes muss zwischen 1 und 100 Zeichen liegen.");

            RuleFor(order => order.Status)
                .NotEmpty().WithMessage("Der Status darf nicht leer sein.")
                .Length(1, 30).WithMessage("Die Länge des Status muss zwischen 1 und 30 Zeichen liegen.");

        }
    }
}
