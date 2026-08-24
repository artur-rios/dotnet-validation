using ArturRios.Validation.Tests.Mock;
using FluentValidation;

namespace ArturRios.Validation.Tests;

/// <summary>
/// A validator that declares an asynchronous rule cannot be run through the synchronous entry points —
/// FluentValidation throws rather than blocking — so the asynchronous helpers are the only way to use one.
/// </summary>
[Trait("Category", "Unit")]
public class AsyncValidationTests
{
    private sealed class AsyncPersonValidator : FluentValidator<Person>
    {
        public AsyncPersonValidator()
        {
            RuleFor(person => person.Name).NotEmpty();
            RuleFor(person => person.Name)
                .MustAsync((name, _) => Task.FromResult(name != "taken"))
                .WithMessage("'Name' is already taken.");
        }
    }

    private readonly AsyncPersonValidator _validator = new();

    private static Person Valid => new() { Name = "Jane", Age = 30 };

    private static Person Taken => new() { Name = "taken", Age = 30 };

    [Fact]
    public async Task GivenAValidModel_WhenValidatingAsynchronously_ThenNoErrorsComeBack()
    {
        Assert.Empty(await _validator.ValidateAndReturnErrorsAsync(Valid));
    }

    [Fact]
    public async Task GivenAModelThatBreaksAnAsyncRule_WhenValidatingAsynchronously_ThenTheRuleReports()
    {
        var errors = await _validator.ValidateAndReturnErrorsAsync(Taken);

        Assert.Contains(errors, error => error.Contains("already taken"));
    }

    [Fact]
    public async Task GivenAModelThatBreaksAnAsyncRule_WhenRemovingSpecialChars_ThenTheMessageIsStripped()
    {
        var errors = await _validator.ValidateAndReturnErrorsAsync(Taken, removeSpecialChars: true);

        Assert.All(errors, error =>
        {
            Assert.DoesNotContain('\'', error);
            Assert.DoesNotContain('.', error);
        });
    }

    [Fact]
    public async Task GivenAModelThatBreaksAnAsyncRule_WhenAskingForAProcessOutput_ThenItFails()
    {
        var output = await _validator.ValidateAndReturnProcessOutputAsync(Taken);

        Assert.False(output.Success);
        Assert.NotEmpty(output.Errors);
    }

    [Fact]
    public async Task GivenAValidModel_WhenAskingForAProcessOutputAsynchronously_ThenItSucceeds()
    {
        var output = await _validator.ValidateAndReturnProcessOutputAsync(Valid);

        Assert.True(output.Success);
    }

    [Fact]
    public async Task GivenAModelThatBreaksAnAsyncRule_WhenAskingForADataOutput_ThenTheModelStillComesBack()
    {
        var model = Taken;

        var output = await _validator.ValidateAndReturnDataOutputAsync(model);

        Assert.False(output.Success);
        Assert.Same(model, output.Data);
    }

    [Fact]
    public async Task GivenACancelledToken_WhenValidatingAsynchronously_ThenTheOperationIsCancelled()
    {
        using var cancellation = new CancellationTokenSource();

        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _validator.ValidateAndReturnErrorsAsync(Valid, cancellationToken: cancellation.Token));
    }

    [Fact]
    public void GivenAValidatorWithAnAsyncRule_WhenValidatingSynchronously_ThenFluentValidationRefuses()
    {
        Assert.Throws<AsyncValidatorInvokedSynchronouslyException>(() => _validator.ValidateAndReturnErrors(Valid));
    }

    [Fact]
    public async Task GivenTheInterface_WhenResolvedThroughIt_ThenBothSynchronousAndAsynchronousHelpersAreReachable()
    {
        IFluentValidator<Person> validator = new PersonValidator();

        Assert.Empty(validator.ValidateAndReturnErrors(Valid));
        Assert.True(validator.ValidateAndReturnProcessOutput(Valid).Success);
        Assert.Empty(await validator.ValidateAndReturnErrorsAsync(Valid));
        Assert.True((await validator.ValidateAndReturnProcessOutputAsync(Valid)).Success);
    }
}
