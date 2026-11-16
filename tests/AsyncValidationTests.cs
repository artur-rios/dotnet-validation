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

    /// <summary>
    /// An asynchronous rule that waits until its token is cancelled, and says when it has started waiting.
    /// </summary>
    private sealed class WaitingValidator : FluentValidator<Person>
    {
        public WaitingValidator()
        {
            RuleFor(person => person.Name).MustAsync(async (_, cancellationToken) =>
            {
                Started.SetResult();
                await Task.Delay(Timeout.Infinite, cancellationToken);
                return true;
            });
        }

        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private readonly AsyncPersonValidator _validator = new();

    private static Person Valid => new() { Name = "Jane", Age = 30 };

    private static Person Taken => new() { Name = "taken", Age = 30 };

    [Fact]
    public async Task GivenAValidModel_WhenValidatingAsynchronously_ThenNoErrorsComeBack()
    {
        Assert.Empty(await _validator.ValidateAndReturnErrorsAsync(Valid, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GivenAModelThatBreaksAnAsyncRule_WhenValidatingAsynchronously_ThenTheRuleReports()
    {
        var errors = await _validator.ValidateAndReturnErrorsAsync(Taken, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(errors, error => error.Contains("already taken"));
    }

    [Fact]
    public async Task GivenAModelThatBreaksAnAsyncRule_WhenRemovingSpecialChars_ThenTheMessageIsStripped()
    {
        var errors = await _validator.ValidateAndReturnErrorsAsync(Taken, removeSpecialChars: true, cancellationToken: TestContext.Current.CancellationToken);

        Assert.All(errors, error =>
        {
            Assert.DoesNotContain('\'', error);
            Assert.DoesNotContain('.', error);
        });
    }

    [Fact]
    public async Task GivenAModelThatBreaksAnAsyncRule_WhenAskingForAProcessOutput_ThenItFails()
    {
        var output = await _validator.ValidateAndReturnProcessOutputAsync(Taken, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(output.Success);
        Assert.NotEmpty(output.Errors);
    }

    [Fact]
    public async Task GivenAValidModel_WhenAskingForAProcessOutputAsynchronously_ThenItSucceeds()
    {
        var output = await _validator.ValidateAndReturnProcessOutputAsync(Valid, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(output.Success);
    }

    [Fact]
    public async Task GivenAModelThatBreaksAnAsyncRule_WhenAskingForADataOutput_ThenTheModelStillComesBack()
    {
        var model = Taken;

        var output = await _validator.ValidateAndReturnDataOutputAsync(model, cancellationToken: TestContext.Current.CancellationToken);

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
    public async Task GivenAValidModel_WhenAskingForADataOutputAsynchronously_ThenItSucceedsAndCarriesTheModel()
    {
        var model = Valid;

        var output = await _validator.ValidateAndReturnDataOutputAsync(model, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(output.Success);
        Assert.Empty(output.Errors);
        Assert.Same(model, output.Data);
    }

    [Fact]
    public async Task GivenAModelThatBreaksOnlyASynchronousRule_WhenValidatingAsynchronously_ThenThatRuleReports()
    {
        var errors = await _validator.ValidateAndReturnErrorsAsync(new Person(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(["'Name' must not be empty."], errors);
    }

    [Fact]
    public async Task GivenAModelThatBreaksAnAsyncRule_WhenAskingForAProcessOutputRemovingSpecialChars_ThenTheErrorsAreStripped()
    {
        var output = await _validator.ValidateAndReturnProcessOutputAsync(Taken, removeSpecialChars: true, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(output.Success);
        Assert.Equal(["Name is already taken"], output.Errors);
    }

    [Fact]
    public async Task GivenAModelThatBreaksAnAsyncRule_WhenAskingForADataOutputRemovingSpecialChars_ThenTheErrorsAreStripped()
    {
        var model = Taken;

        var output = await _validator.ValidateAndReturnDataOutputAsync(model, removeSpecialChars: true, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(output.Success);
        Assert.Equal(["Name is already taken"], output.Errors);
        Assert.Same(model, output.Data);
    }

    [Fact]
    public async Task GivenACancelledToken_WhenAskingForAProcessOutputAsynchronously_ThenTheOperationIsCancelled()
    {
        using var cancellation = new CancellationTokenSource();

        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _validator.ValidateAndReturnProcessOutputAsync(Valid, cancellationToken: cancellation.Token));
    }

    [Fact]
    public async Task GivenACancelledToken_WhenAskingForADataOutputAsynchronously_ThenTheOperationIsCancelled()
    {
        using var cancellation = new CancellationTokenSource();

        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _validator.ValidateAndReturnDataOutputAsync(Valid, cancellationToken: cancellation.Token));
    }

    [Fact]
    public async Task GivenAnAsyncRuleInProgress_WhenTheTokenIsCancelled_ThenTheRuleSeesItAndTheValidationIsCancelled()
    {
        var validator = new WaitingValidator();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        var validation = validator.ValidateAndReturnErrorsAsync(Valid, cancellationToken: cancellation.Token);
        await validator.Started.Task.WaitAsync(TestContext.Current.CancellationToken);
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => validation);
    }

    [Fact]
    public async Task GivenANullModel_WhenValidatingAsynchronously_ThenFluentValidationRefusesIt()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _validator.ValidateAndReturnErrorsAsync(null!, cancellationToken: TestContext.Current.CancellationToken));
    }
}
