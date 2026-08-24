using ArturRios.Output;
using ArturRios.Validation.Tests.Mock;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ArturRios.Validation.Tests.Functional;

/// <summary>
/// Resolves the validator the way an application does — out of a real service collection, behind
/// <see cref="IFluentValidator{T}"/> and behind FluentValidation's own <see cref="IValidator{T}"/> — and
/// drives a whole request-shaped flow through it.
/// </summary>
[Trait("Category", "Functional")]
public class ValidationThroughDependencyInjectionTests
{
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();

        services.AddSingleton<PersonValidator>();
        services.AddSingleton<IFluentValidator<Person>>(provider => provider.GetRequiredService<PersonValidator>());
        services.AddSingleton<IValidator<Person>>(provider => provider.GetRequiredService<PersonValidator>());

        return services.BuildServiceProvider();
    }

    [Fact]
    public void GivenAValidatorRegisteredBehindTheInterface_WhenResolved_ThenTheSameInstanceBacksBothContracts()
    {
        using var provider = BuildProvider();

        var fluent = provider.GetRequiredService<IFluentValidator<Person>>();
        var validator = provider.GetRequiredService<IValidator<Person>>();

        Assert.Same(fluent, validator);
    }

    [Fact]
    public void GivenAnInvalidModel_WhenValidatedThroughTheInterface_ThenTheEnvelopeCarriesEveryFailure()
    {
        using var provider = BuildProvider();

        var validator = provider.GetRequiredService<IFluentValidator<Person>>();

        ProcessOutput output = validator.ValidateAndReturnProcessOutput(new Person { Name = string.Empty, Age = 0 });

        Assert.False(output.Success);
        Assert.Equal(2, output.Errors.Count);
        Assert.Contains(output.Errors, error => error.Contains("Name"));
        Assert.Contains(output.Errors, error => error.Contains("Age"));
    }

    [Fact]
    public void GivenAValidModel_WhenValidatedThroughFluentValidationsOwnContract_ThenItPasses()
    {
        using var provider = BuildProvider();

        var validator = provider.GetRequiredService<IValidator<Person>>();

        Assert.True(validator.Validate(new Person { Name = "Jane", Age = 30 }).IsValid);
    }

    [Fact]
    public void GivenTheConcreteValidator_WhenAskedForADataOutput_ThenTheModelComesBackInsideTheEnvelope()
    {
        using var provider = BuildProvider();

        var validator = provider.GetRequiredService<PersonValidator>();
        var model = new Person { Name = string.Empty, Age = 0 };

        var output = validator.ValidateAndReturnDataOutput(model);

        Assert.False(output.Success);
        Assert.Same(model, output.Data);
    }

    [Fact]
    public async Task GivenABatchOfModels_WhenValidatedConcurrentlyThroughOneInstance_ThenEachGetsItsOwnResult()
    {
        using var provider = BuildProvider();

        var validator = provider.GetRequiredService<IFluentValidator<Person>>();

        var people = Enumerable.Range(0, 50)
            .Select(i => new Person { Name = i % 2 == 0 ? "Jane" : string.Empty, Age = i % 2 == 0 ? 30 : 0 })
            .ToArray();

        var outputs = await Task.WhenAll(people.Select(person =>
            Task.Run(() => validator.ValidateAndReturnProcessOutput(person))));

        Assert.Equal(25, outputs.Count(output => output.Success));
        Assert.All(outputs.Where(output => !output.Success), output => Assert.Equal(2, output.Errors.Count));
    }

    [Fact]
    public void GivenTheStrippedMessages_WhenComparedWithTheOriginals_ThenOnlyTheSpecialCharsDiffer()
    {
        using var provider = BuildProvider();

        var validator = provider.GetRequiredService<IFluentValidator<Person>>();
        var model = new Person { Name = string.Empty, Age = 0 };

        var raw = validator.ValidateAndReturnErrors(model);
        var stripped = validator.ValidateAndReturnErrors(model, removeSpecialChars: true);

        Assert.Equal(
            raw.Select(message => message.Replace("'", string.Empty).Replace(".", string.Empty)),
            stripped);
    }
}
