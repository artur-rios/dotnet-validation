using System.Text.RegularExpressions;
using ArturRios.Output;
using FluentValidation;
using FluentValidation.Results;

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
    /// <param name="removeSpecialChars">Strips the quoting apostrophes and sentence-ending full stops from the messages when <see langword="true"/>.</param>
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
    /// <param name="removeSpecialChars">Strips the quoting apostrophes and sentence-ending full stops from the messages when <see langword="true"/>.</param>
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
    /// Projects a validation result onto its messages, one per failure, optionally stripped of the default
    /// special characters.
    /// </summary>
    private static string[] Messages(FluentValidation.Results.ValidationResult result, bool removeSpecialChars) =>
        [.. result.Errors.Select(failure => Message(failure, removeSpecialChars))];

    /// <summary>
    /// The message reported for one failure — never blank.
    /// </summary>
    /// <remarks>
    /// <see cref="ProcessOutput.AddErrors"/> drops blank entries, so a failure whose message is blank — a
    /// <c>WithMessage</c> callback that returned nothing, or a message made only of the characters
    /// <paramref name="removeSpecialChars"/> strips — would vanish from the envelope and leave it reporting
    /// <see cref="ProcessOutput.Success"/> for an invalid model. Such a failure is reported with
    /// FluentValidation's own wording for a broken condition instead.
    /// </remarks>
    private static string Message(ValidationFailure failure, bool removeSpecialChars)
    {
        var message = Clean(failure.ErrorMessage, removeSpecialChars);

        if (!string.IsNullOrWhiteSpace(message))
        {
            return message;
        }

        var fallback = string.IsNullOrWhiteSpace(failure.PropertyName)
            ? "The specified condition was not met."
            : $"The specified condition was not met for '{failure.PropertyName}'.";

        return Clean(fallback, removeSpecialChars);
    }

    private static string Clean(string? message, bool removeSpecialChars) =>
        removeSpecialChars ? SpecialChars().Replace(message ?? string.Empty, string.Empty) : message ?? string.Empty;

    /// <summary>
    /// Matches the quoting apostrophes and the sentence-ending full stops FluentValidation puts in its
    /// default messages — <c>'Name' must not be empty.</c> — and nothing that belongs to the text itself.
    /// </summary>
    /// <remarks>
    /// An apostrophe between two letters is part of a word (<c>can't</c>, <c>owner's</c>) and a full stop
    /// followed by anything but whitespace is part of a value (<c>0.5</c>, <c>example.org</c>); stripping
    /// those changed what the message said — <c>greater than '0.5'</c> read <c>greater than 05</c>.
    /// </remarks>
    [GeneratedRegex(@"(?<!\p{L})'|'(?!\p{L})|\.(?!\S)", RegexOptions.None, MatchTimeoutMilliseconds)]
    private static partial Regex SpecialChars();
}
