using ArturRios.Validation.Tests.Mock;
using FluentValidation;
using FluentValidation.Results;

namespace ArturRios.Validation.Tests;

/// <summary>
/// The envelope's <c>Success</c> is documented to be true exactly when the model is valid, so no failure may
/// disappear on the way into it.
/// </summary>
[Trait("Category", "Unit")]
public class BlankMessageTests
{
    private sealed class BlankMessageValidator : FluentValidator<Person>
    {
        public BlankMessageValidator()
        {
            RuleFor(person => person.Name).NotEmpty().WithMessage(_ => string.Empty);
            RuleFor(person => person.Age).GreaterThan(0).WithMessage("'.");
        }
    }

    /// <summary>
    /// Reports one failure built by hand, as a custom rule can: its property name and message may be blank or null,
    /// which FluentValidation's own rules never produce.
    /// </summary>
    private sealed class HandMadeFailureValidator : FluentValidator<Person>
    {
        public HandMadeFailureValidator(string? propertyName, string? message)
        {
            RuleFor(person => person).Custom((_, context) => context.AddFailure(new ValidationFailure(propertyName, message)));
        }
    }

    private readonly BlankMessageValidator _validator = new();

    [Fact]
    public void GivenARuleWithABlankMessage_WhenValidatingAndReturningProcessOutput_ThenTheOutputStillFails()
    {
        var output = _validator.ValidateAndReturnProcessOutput(new Person { Age = 30 });

        Assert.False(output.Success);
        Assert.Single(output.Errors);
    }

    [Fact]
    public void GivenAMessageThatIsBlankOnceStripped_WhenValidatingAndReturningDataOutput_ThenTheOutputStillFails()
    {
        var output = _validator.ValidateAndReturnDataOutput(new Person { Name = "Jane" }, removeSpecialChars: true);

        Assert.False(output.Success);
        Assert.Single(output.Errors);
    }

    [Fact]
    public async Task GivenARuleWithABlankMessage_WhenValidatingAsync_ThenEveryFailureIsReported()
    {
        var errors = await _validator.ValidateAndReturnErrorsAsync(new Person(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, errors.Length);
        Assert.All(errors, error => Assert.False(string.IsNullOrWhiteSpace(error)));
    }

    [Fact]
    public void GivenARuleWithABlankMessage_WhenValidating_ThenFluentValidationsWordingForABrokenConditionIsReported()
    {
        var errors = new FixedMessageValidator(string.Empty).ValidateAndReturnErrors(new Person());

        Assert.Equal(["The specified condition was not met for 'Name'."], errors);
    }

    [Fact]
    public void GivenARuleWithABlankMessage_WhenRemovingSpecialChars_ThenTheFallbackIsStrippedToo()
    {
        var errors = new FixedMessageValidator(string.Empty).ValidateAndReturnErrors(new Person(), removeSpecialChars: true);

        Assert.Equal(["The specified condition was not met for Name"], errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void GivenARuleWithANullOrWhiteSpaceMessage_WhenValidating_ThenTheFallbackIsReported(string? message)
    {
        var validator = new FixedMessageValidator(message);

        Assert.Equal(["The specified condition was not met for 'Name'."], validator.ValidateAndReturnErrors(new Person()));
        Assert.Equal(["The specified condition was not met for Name"], validator.ValidateAndReturnErrors(new Person(), removeSpecialChars: true));
    }

    [Fact]
    public void GivenAMessageThatIsBlankOnceStripped_WhenNotRemovingSpecialChars_ThenItComesBackAsWritten()
    {
        var errors = _validator.ValidateAndReturnErrors(new Person { Name = "Jane" });

        Assert.Equal(["'."], errors);
    }

    [Fact]
    public void GivenAMessageThatIsBlankOnceStripped_WhenRemovingSpecialChars_ThenTheStrippedFallbackIsReported()
    {
        var errors = _validator.ValidateAndReturnErrors(new Person { Name = "Jane" }, removeSpecialChars: true);

        Assert.Equal(["The specified condition was not met for Age"], errors);
    }

    [Theory]
    [InlineData("Name", null, "The specified condition was not met for 'Name'.", "The specified condition was not met for Name")]
    [InlineData("", "", "The specified condition was not met.", "The specified condition was not met")]
    [InlineData(" ", null, "The specified condition was not met.", "The specified condition was not met")]
    [InlineData(null, null, "The specified condition was not met.", "The specified condition was not met")]
    public void GivenAHandMadeFailureWithABlankMessage_WhenValidating_ThenTheFallbackNamesThePropertyIfItHasOne(
        string? propertyName, string? message, string expected, string expectedStripped)
    {
        var validator = new HandMadeFailureValidator(propertyName, message);

        Assert.Equal([expected], validator.ValidateAndReturnErrors(new Person()));
        Assert.Equal([expectedStripped], validator.ValidateAndReturnErrors(new Person(), removeSpecialChars: true));
    }
}
