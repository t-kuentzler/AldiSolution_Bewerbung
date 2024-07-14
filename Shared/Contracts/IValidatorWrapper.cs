namespace Shared.Contracts;

public interface IValidatorWrapper<T>
{
    Task ValidateAndThrowAsync(T instance);
}