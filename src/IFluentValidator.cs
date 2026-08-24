using ArturRios.Output;
using FluentValidation;

namespace ArturRios.Validation;

/// <summary>
/// Abstraction over <see cref="FluentValidator{T}"/> for dependency injection and testing, layered on
/// FluentValidation's <see cref="IValidator{T}"/>.
/// </summary>
/// <typeparam name="T">The model type being validated.</typeparam>
/// <remarks>
/// The type parameter is contravariant, so a validator for a base type can stand in for one of a derived
/// type. That is why <see cref="FluentValidator{T}.ValidateAndReturnDataOutput"/> is not declared here:
/// it returns a <c>DataOutput&lt;T&gt;</c>, which puts <typeparamref name="T"/> in an output position and
/// contravariance forbids that. Take the concrete <see cref="FluentValidator{T}"/> when the validated model
/// has to come back inside the envelope.
/// </remarks>
public interface IFluentValidator<in T> : IValidator<T>
{
    /// <summary>
    /// Validates <paramref name="model"/> and returns the failure messages.
    /// </summary>
    /// <param name="model">The model to validate.</param>
    /// <param name="removeSpecialChars">
    /// When <see langword="true"/>, strips the apostrophes and full stops FluentValidation puts in its
    /// default messages, turning <c>"'Name' must not be empty."</c> into <c>"Name must not be empty"</c>.
    /// </param>
    /// <returns>One message per broken rule, or an empty array when the model is valid.</returns>
    string[] ValidateAndReturnErrors(T model, bool removeSpecialChars = false);

    /// <summary>
    /// Validates <paramref name="model"/> and returns the failures as a <see cref="ProcessOutput"/> envelope.
    /// </summary>
    /// <param name="model">The model to validate.</param>
    /// <param name="removeSpecialChars">Strips the apostrophes and full stops from the messages when <see langword="true"/>.</param>
    /// <returns>
    /// An envelope whose <see cref="ProcessOutput.Success"/> is <see langword="true"/> exactly when the
    /// model is valid.
    /// </returns>
    ProcessOutput ValidateAndReturnProcessOutput(T model, bool removeSpecialChars = false);

    /// <summary>
    /// Asynchronously validates <paramref name="model"/> and returns the failure messages.
    /// </summary>
    /// <param name="model">The model to validate.</param>
    /// <param name="removeSpecialChars">Strips the apostrophes and full stops from the messages when <see langword="true"/>.</param>
    /// <param name="cancellationToken">Cancels the validation.</param>
    /// <returns>One message per broken rule, or an empty array when the model is valid.</returns>
    /// <remarks>
    /// Use this whenever the validator declares any asynchronous rule — <c>MustAsync</c>,
    /// <c>CustomAsync</c> and the like. FluentValidation refuses to run such a rule from the synchronous
    /// overload and throws <see cref="AsyncValidatorInvokedSynchronouslyException"/> instead.
    /// </remarks>
    Task<string[]> ValidateAndReturnErrorsAsync(
        T model,
        bool removeSpecialChars = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously validates <paramref name="model"/> and returns the failures as a
    /// <see cref="ProcessOutput"/> envelope.
    /// </summary>
    /// <param name="model">The model to validate.</param>
    /// <param name="removeSpecialChars">Strips the apostrophes and full stops from the messages when <see langword="true"/>.</param>
    /// <param name="cancellationToken">Cancels the validation.</param>
    /// <returns>
    /// An envelope whose <see cref="ProcessOutput.Success"/> is <see langword="true"/> exactly when the
    /// model is valid.
    /// </returns>
    Task<ProcessOutput> ValidateAndReturnProcessOutputAsync(
        T model,
        bool removeSpecialChars = false,
        CancellationToken cancellationToken = default);
}
