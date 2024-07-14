using FluentValidation;
using Shared.Contracts;

namespace Shared.Validation;

public class ValidatorWrapper<T> : IValidatorWrapper<T>
{
    private readonly IValidator<T> _validator;

    public ValidatorWrapper(IValidator<T> validator)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public Task ValidateAndThrowAsync(T instance)
    {
        return _validator.ValidateAndThrowAsync(instance);
    }
}