using System.Linq;
using ArturRios.Validation.Tests.Mock;

namespace ArturRios.Validation.Tests;

[Trait("Category", "Unit")]
public class FluentValidatorTests
{
    private readonly PersonValidator _validator = new();

    private static Person ValidPerson => new() { Name = "Jane", Age = 30 };

    private static Person InvalidPerson => new() { Name = string.Empty, Age = 0 };

    [Fact]
    public void GivenValidModel_WhenValidatingAndReturningErrors_ThenNoErrorsAreReturned()
    {
        var errors = _validator.ValidateAndReturnErrors(ValidPerson);

        Assert.Empty(errors);
    }

    [Fact]
    public void GivenInvalidModel_WhenValidatingAndReturningErrors_ThenErrorsAreReturnedForEachBrokenRule()
    {
        var errors = _validator.ValidateAndReturnErrors(InvalidPerson);

        Assert.Equal(2, errors.Length);
        Assert.Contains(errors, e => e.Contains("Name"));
        Assert.Contains(errors, e => e.Contains("Age"));
    }

    [Fact]
    public void GivenInvalidModel_WhenNotRemovingSpecialChars_ThenErrorMessagesKeepDefaultSpecialChars()
    {
        var errors = _validator.ValidateAndReturnErrors(InvalidPerson);

        // FluentValidation default messages are like "'Name' must not be empty."
        Assert.Contains(errors, e => e.Contains('\'') || e.Contains('.'));
    }

    [Fact]
    public void GivenInvalidModel_WhenRemovingSpecialChars_ThenApostrophesAndDotsAreStripped()
    {
        var errors = _validator.ValidateAndReturnErrors(InvalidPerson, removeSpecialChars: true);

        Assert.NotEmpty(errors);
        Assert.All(errors, e =>
        {
            Assert.DoesNotContain('\'', e);
            Assert.DoesNotContain('.', e);
        });
    }

    [Fact]
    public void GivenInvalidModel_WhenRemovingSpecialChars_ThenMessageContentIsOtherwisePreserved()
    {
        var withChars = _validator.ValidateAndReturnErrors(InvalidPerson);
        var withoutChars = _validator.ValidateAndReturnErrors(InvalidPerson, removeSpecialChars: true);

        var expected = withChars.Select(e => e.Replace("'", string.Empty).Replace(".", string.Empty)).ToArray();

        Assert.Equal(expected, withoutChars);
    }

    [Fact]
    public void GivenValidModel_WhenValidatingAndReturningProcessOutput_ThenOutputIsSuccessfulWithoutErrors()
    {
        var output = _validator.ValidateAndReturnProcessOutput(ValidPerson);

        Assert.True(output.Success);
        Assert.Empty(output.Errors);
    }

    [Fact]
    public void GivenInvalidModel_WhenValidatingAndReturningProcessOutput_ThenOutputFailsWithErrors()
    {
        var output = _validator.ValidateAndReturnProcessOutput(InvalidPerson);

        Assert.False(output.Success);
        Assert.Equal(2, output.Errors.Count);
    }

    [Fact]
    public void GivenInvalidModel_WhenValidatingProcessOutputRemovingSpecialChars_ThenErrorsHaveNoSpecialChars()
    {
        var output = _validator.ValidateAndReturnProcessOutput(InvalidPerson, removeSpecialChars: true);

        Assert.False(output.Success);
        Assert.All(output.Errors, e =>
        {
            Assert.DoesNotContain('\'', e);
            Assert.DoesNotContain('.', e);
        });
    }

    [Fact]
    public void GivenValidModel_WhenValidatingAndReturningDataOutput_ThenOutputIsSuccessfulAndCarriesData()
    {
        var model = ValidPerson;

        var output = _validator.ValidateAndReturnDataOutput(model);

        Assert.True(output.Success);
        Assert.Empty(output.Errors);
        Assert.Same(model, output.Data);
    }

    [Fact]
    public void GivenInvalidModel_WhenValidatingAndReturningDataOutput_ThenOutputFailsButStillCarriesData()
    {
        var model = InvalidPerson;

        var output = _validator.ValidateAndReturnDataOutput(model);

        Assert.False(output.Success);
        Assert.Equal(2, output.Errors.Count);
        Assert.Same(model, output.Data);
    }

    [Fact]
    public void GivenInvalidModel_WhenValidatingDataOutputRemovingSpecialChars_ThenErrorsHaveNoSpecialChars()
    {
        var output = _validator.ValidateAndReturnDataOutput(InvalidPerson, removeSpecialChars: true);

        Assert.False(output.Success);
        Assert.All(output.Errors, e =>
        {
            Assert.DoesNotContain('\'', e);
            Assert.DoesNotContain('.', e);
        });
    }
}
