using System.Text.RegularExpressions;
using ArturRios.Output;
using FluentValidation;

namespace ArturRios.Validation;

/// <summary>
/// Base validator that turns a FluentValidation result into the shapes an application consumes: a plain
/// array of messages, or an <see cref="ArturRios.Output"/> envelope.
/// </summary>
/// <typeparam name="T">The model type being validated.</typeparam>
/// <remarks>
/// Subclass it and declare the rules in the constructor exactly as with
/// <see cref="AbstractValidator{T}"/>; the helpers below come for free. Validation never throws for a model
/// that simply fails its rules — the failures arrive on the result, which is the family's convention.
/// </remarks>
public partial class FluentValidator<T> : AbstractValidator<T>, IFluentValidator<T>
{
    /// <summary>
    /// Upper bound, in milliseconds, on stripping the special characters from one message, so a pathological
    /// message can never spin.
    /// </summary>
    private const int MatchTimeoutMilliseconds = 100;

    /// <inheritdoc />
    public string[] ValidateAndReturnErrors(T model, bool removeSpecialChars = false) =>
        Messages(Validate(model), removeSpecialChars);

    /// <inheritdoc />
    public ProcessOutput ValidateAndReturnProcessOutput(T model, bool removeSpecialChars = false) =>
        ProcessOutput.New.WithErrors(ValidateAndReturnErrors(model, removeSpecialChars));

    /// <summary>
    /// Validates <paramref name="model"/> and returns the failures as a <see cref="DataOutput{T}"/> envelope
    /// that also carries the model back.
    /// </summary>
    /// <param name="model">The model to validate.</param>
    /// <param name="removeSpecialChars">Strips the apostrophes and full stops from the messages when <see langword="true"/>.</param>
    /// <returns>
    /// An envelope carrying <paramref name="model"/> whether or not it is valid, so a caller can report the
    /// failures alongside what produced them.
    /// </returns>
    /// <remarks>
    /// This one is absent from <see cref="IFluentValidator{T}"/>: the interface is contravariant in
    /// <typeparamref name="T"/>, and a <see cref="DataOutput{T}"/> return puts the type parameter in an
    /// output position.
    /// </remarks>
    public DataOutput<T> ValidateAndReturnDataOutput(T model, bool removeSpecialChars = false) =>
        DataOutput<T>.New
            .WithData(model)
            .WithErrors(ValidateAndReturnErrors(model, removeSpecialChars));

    /// <inheritdoc />
    public async Task<string[]> ValidateAndReturnErrorsAsync(
        T model,
        bool removeSpecialChars = false,
        CancellationToken cancellationToken = default) =>
        Messages(await ValidateAsync(model, cancellationToken).ConfigureAwait(false), removeSpecialChars);

    /// <inheritdoc />
    public async Task<ProcessOutput> ValidateAndReturnProcessOutputAsync(
        T model,
        bool removeSpecialChars = false,
        CancellationToken cancellationToken = default) =>
        ProcessOutput.New.WithErrors(
            await ValidateAndReturnErrorsAsync(model, removeSpecialChars, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Asynchronously validates <paramref name="model"/> and returns the failures as a
    /// <see cref="DataOutput{T}"/> envelope that also carries the model back.
    /// </summary>
    /// <param name="model">The model to validate.</param>
    /// <param name="removeSpecialChars">Strips the apostrophes and full stops from the messages when <see langword="true"/>.</param>
    /// <param name="cancellationToken">Cancels the validation.</param>
    /// <returns>An envelope carrying <paramref name="model"/> whether or not it is valid.</returns>
    public async Task<DataOutput<T>> ValidateAndReturnDataOutputAsync(
        T model,
        bool removeSpecialChars = false,
        CancellationToken cancellationToken = default) =>
        DataOutput<T>.New
            .WithData(model)
            .WithErrors(await ValidateAndReturnErrorsAsync(model, removeSpecialChars, cancellationToken)
                .ConfigureAwait(false));

    /// <summary>
    /// Projects a validation result onto its messages, optionally stripped of the default special characters.
    /// </summary>
    private static string[] Messages(FluentValidation.Results.ValidationResult result, bool removeSpecialChars)
    {
        var messages = result.Errors.Select(error => error.ErrorMessage);

        if (removeSpecialChars)
        {
            messages = messages.Select(message => SpecialChars().Replace(message, string.Empty));
        }

        return [.. messages];
    }

    /// <summary>
    /// Matches the apostrophes and full stops FluentValidation puts in its default messages.
    /// </summary>
    [GeneratedRegex(@"['.]", RegexOptions.None, MatchTimeoutMilliseconds)]
    private static partial Regex SpecialChars();
}
