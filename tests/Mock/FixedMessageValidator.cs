using FluentValidation;

namespace ArturRios.Validation.Tests.Mock;

/// <summary>
/// A validator with one rule that always fails, on <see cref="Person.Name"/>, with the message it is given: the
/// message reaches the helpers exactly as written, so a test can choose what the cleanup sees.
/// </summary>
public sealed class FixedMessageValidator : FluentValidator<Person>
{
    public FixedMessageValidator(string? message)
    {
        RuleFor(person => person.Name).Must(_ => false).WithMessage(_ => message!);
    }
}
