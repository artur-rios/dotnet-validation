using ArturRios.Validation.Tests.Mock;
using FluentValidation;

namespace ArturRios.Validation.Tests;

/// <summary>
/// What <c>removeSpecialChars</c> strips, and what it must leave alone: the quotes and the closing full stop
/// FluentValidation wraps around a message, not the data inside it.
/// </summary>
[Trait("Category", "Unit")]
public class MessageCleanupTests
{
    private sealed class Order
    {
        public decimal Amount { get; init; }

        public string Owner { get; init; } = string.Empty;

        public string Site { get; init; } = string.Empty;
    }

    private sealed class OrderValidator : FluentValidator<Order>
    {
        public OrderValidator()
        {
            RuleFor(order => order.Amount).GreaterThan(0.5m);
            RuleFor(order => order.Owner).NotEqual("x").WithMessage("The owner's name can't be 'x'.");
            RuleFor(order => order.Site).Must(site => site.EndsWith(".org")).WithMessage("'Site' must end in '.org', e.g. example.org.");
        }
    }

    private readonly OrderValidator _validator = new();

    private static Order Invalid => new() { Amount = 0.25m, Owner = "x", Site = "example.com" };

    [Fact]
    public void GivenAComparisonWithADecimalValue_WhenRemovingSpecialChars_ThenTheDecimalPointIsKept()
    {
        var errors = _validator.ValidateAndReturnErrors(Invalid, removeSpecialChars: true);

        // "'Amount' must be greater than '0.5'." used to become "Amount must be greater than 05".
        Assert.Contains("Amount must be greater than 0.5", errors);
    }

    [Fact]
    public void GivenAMessageWithContractions_WhenRemovingSpecialChars_ThenTheApostrophesInsideWordsAreKept()
    {
        var errors = _validator.ValidateAndReturnErrors(Invalid, removeSpecialChars: true);

        Assert.Contains("The owner's name can't be x", errors);
    }

    [Fact]
    public void GivenAMessageWithFullStopsInsideWords_WhenRemovingSpecialChars_ThenOnlyTheSentenceEndingOnesGo()
    {
        var errors = _validator.ValidateAndReturnErrors(Invalid, removeSpecialChars: true);

        Assert.Contains("Site must end in .org, e.g example.org", errors);
    }

    [Fact]
    public void GivenTheDefaultMessages_WhenRemovingSpecialChars_ThenTheyAreStrippedAsBefore()
    {
        var errors = new PersonValidator().ValidateAndReturnErrors(new Person(), removeSpecialChars: true);

        Assert.Contains("Name must not be empty", errors);
        Assert.Contains("Age must be greater than 0", errors);
    }
}

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
        var errors = await _validator.ValidateAndReturnErrorsAsync(new Person());

        Assert.Equal(2, errors.Length);
        Assert.All(errors, error => Assert.False(string.IsNullOrWhiteSpace(error)));
    }
}
