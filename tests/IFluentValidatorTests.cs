using ArturRios.Validation.Tests.Mock;

namespace ArturRios.Validation.Tests;

/// <summary>
/// The contract an application takes the validator through: every helper it declares is reachable behind it.
/// </summary>
[Trait("Category", "Unit")]
public class IFluentValidatorTests
{
    private sealed class Employee : Person;

    private static Person Valid => new() { Name = "Jane", Age = 30 };

    [Fact]
    public async Task GivenTheInterface_WhenResolvedThroughIt_ThenBothSynchronousAndAsynchronousHelpersAreReachable()
    {
        IFluentValidator<Person> validator = new PersonValidator();

        Assert.Empty(validator.ValidateAndReturnErrors(Valid));
        Assert.True(validator.ValidateAndReturnProcessOutput(Valid).Success);
        Assert.Empty(await validator.ValidateAndReturnErrorsAsync(Valid, cancellationToken: TestContext.Current.CancellationToken));
        Assert.True((await validator.ValidateAndReturnProcessOutputAsync(Valid, cancellationToken: TestContext.Current.CancellationToken)).Success);
    }

    [Fact]
    public async Task GivenTheInterface_WhenRemovingSpecialChars_ThenEveryHelperStripsTheMessages()
    {
        IFluentValidator<Person> validator = new PersonValidator();
        var model = new Person { Name = "Jane" };
        string[] expected = ["Age must be greater than 0"];

        Assert.Equal(expected, validator.ValidateAndReturnErrors(model, removeSpecialChars: true));
        Assert.Equal(expected, validator.ValidateAndReturnProcessOutput(model, removeSpecialChars: true).Errors);
        Assert.Equal(expected, await validator.ValidateAndReturnErrorsAsync(model, removeSpecialChars: true, TestContext.Current.CancellationToken));
        Assert.Equal(expected, (await validator.ValidateAndReturnProcessOutputAsync(model, removeSpecialChars: true, TestContext.Current.CancellationToken)).Errors);
    }

    [Fact]
    public async Task GivenAValidatorOfABaseType_WhenUsedAsTheValidatorOfADerivedType_ThenItValidatesTheDerivedModel()
    {
        // The interface is contravariant: a validator written for Person stands in for one of Employee.
        IFluentValidator<Employee> validator = new PersonValidator();
        var employee = new Employee { Name = string.Empty, Age = 0 };

        Assert.Equal(2, validator.ValidateAndReturnErrors(employee).Length);
        Assert.False(validator.ValidateAndReturnProcessOutput(employee).Success);
        Assert.Equal(2, (await validator.ValidateAndReturnErrorsAsync(employee, cancellationToken: TestContext.Current.CancellationToken)).Length);
        Assert.False((await validator.ValidateAndReturnProcessOutputAsync(employee, cancellationToken: TestContext.Current.CancellationToken)).Success);
    }
}
