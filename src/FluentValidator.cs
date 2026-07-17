using System.Text.RegularExpressions;
using ArturRios.Output;
using FluentValidation;

namespace ArturRios.Validation;

public class FluentValidator<T> : AbstractValidator<T>, IFluentValidator<T>
{
    private static readonly char[] DefaultErrorMessageSpecialChars = ['\'', '.'];

    private readonly Regex _specialCharsRegex = new($"[{Regex.Escape(new string(DefaultErrorMessageSpecialChars))}]");


    public string[] ValidateAndReturnErrors(T model, bool removeSpecialChars = false)
    {
        var validationResult = Validate(model);

        var errorMessages = validationResult.Errors.Select(e => e.ErrorMessage).ToArray();

        if (removeSpecialChars)
        {
            errorMessages = errorMessages.Select(msg => _specialCharsRegex.Replace(msg, string.Empty)).ToArray();
        }

        return validationResult.IsValid ? [] : errorMessages;
    }

    public ProcessOutput ValidateAndReturnProcessOutput(T model, bool removeSpecialChars = false)
    {
        var errorMessages = ValidateAndReturnErrors(model, removeSpecialChars);

        return ProcessOutput.New
            .WithErrors(errorMessages);
    }

    public DataOutput<T> ValidateAndReturnDataOutput(T model, bool removeSpecialChars = false)
    {
        var errorMessages = ValidateAndReturnErrors(model, removeSpecialChars);

        return DataOutput<T>.New
            .WithData(model)
            .WithErrors(errorMessages);
    }
}
