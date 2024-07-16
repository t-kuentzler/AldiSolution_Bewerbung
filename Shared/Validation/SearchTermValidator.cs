using FluentValidation;
using Shared.Models;

namespace Shared.Validation
{
    public class SearchTermValidator : AbstractValidator<SearchTerm>
    {
        public SearchTermValidator()
        {
            RuleFor(searchTerm => searchTerm.value)
                .MaximumLength(30).WithMessage("Der Suchbegriff darf nicht mehr als 30 Zeichen haben.");
        }
    }
}