using System.Globalization;
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

    [Theory]
    [InlineData("'Name' must not be empty.", "Name must not be empty")]
    [InlineData("'Name' must be 'x'", "Name must be x")]
    [InlineData("'5' is not allowed.", "5 is not allowed")]
    [InlineData("It can't be done", "It can't be done")]
    [InlineData("L'été est là.", "L'été est là")]
    [InlineData("The users' list is full.", "The users list is full")]
    [InlineData("Use 0.5 or 1.5.", "Use 0.5 or 1.5")]
    [InlineData("Visit example.org.", "Visit example.org")]
    [InlineData("First. Second.", "First Second")]
    [InlineData("Line one.\nLine two.", "Line one\nLine two")]
    [InlineData("Wait...", "Wait..")]
    [InlineData("Nothing to strip", "Nothing to strip")]
    public void GivenAMessage_WhenRemovingSpecialChars_ThenOnlyTheQuotingApostrophesAndTheSentenceEndingFullStopsGo(
        string message, string expected)
    {
        var errors = new FixedMessageValidator(message).ValidateAndReturnErrors(new Person(), removeSpecialChars: true);

        Assert.Equal([expected], errors);
    }

    [Fact]
    public void GivenAMessageWithSpecialChars_WhenNotRemovingSpecialChars_ThenItComesBackVerbatim()
    {
        const string message = "'5' is not allowed. Use 0.5 or can't.";

        var errors = new FixedMessageValidator(message).ValidateAndReturnErrors(new Person());

        Assert.Equal([message], errors);
    }

    [Fact]
    public void GivenThePtBrCulture_WhenRemovingSpecialChars_ThenTheTranslatedMessageIsCleanedAndKeepsItsDecimalComma()
    {
        using var culture = new CultureScope(CultureInfo.GetCultureInfo("pt-BR"));

        var errors = _validator.ValidateAndReturnErrors(Invalid, removeSpecialChars: true);

        var amount = Assert.Single(errors, error => error.StartsWith("Amount ", StringComparison.Ordinal));
        Assert.DoesNotContain("greater than", amount);
        Assert.Contains("0,5", amount);
        Assert.DoesNotContain('\'', amount);
        Assert.False(amount.EndsWith('.'));
    }
}
