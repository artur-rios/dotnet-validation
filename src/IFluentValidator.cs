using FluentValidation;

namespace ArturRios.Validation;

public interface IFluentValidator<in T> : IValidator<T>
{
    string[] ValidateAndReturnErrors(T model, bool removeSpecialChars = false);
}
