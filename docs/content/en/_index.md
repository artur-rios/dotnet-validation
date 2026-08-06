---
title: ArturRios.Validation
linkTitle: Home
---

{{< blocks/cover title="ArturRios.Validation" height="auto" color="primary" >}}
<p class="lead mt-4">
A thin, opinionated model-validation layer for .NET built on top of [FluentValidation](https://docs.fluentvalidation.net/). It wraps FluentValidation's `AbstractValidator<T>` in a `FluentValidator<T>` base class that turns validation results into the shapes an application actually consumes: a plain array of error messages, or an [`ArturRios.Output`](https://www.nuget.org/packages/ArturRios.Output) `ProcessOutput` / `DataOutput<T>` envelope — with optional stripping of the quotes and periods FluentValidation puts in its default messages.
</p>
<a class="btn btn-lg btn-secondary me-3 mb-4" href="docs/">
  Documentation <i class="fas fa-arrow-alt-circle-right ms-2"></i>
</a>
<a class="btn btn-lg btn-secondary me-3 mb-4" href="https://github.com/artur-rios/dotnet-validation">
  GitHub <i class="fab fa-github ms-2"></i>
</a>
{{< /blocks/cover >}}

{{% blocks/lead color="light" %}}
A thin, opinionated model-validation layer for .NET built on top of [FluentValidation](https://docs.fluentvalidation.net/). It wraps FluentValidation's `AbstractValidator<T>` in a `FluentValidator<T>` base class that turns validation results into the shapes an application actually consumes: a plain array of error messages, or an [`ArturRios.Output`](https://www.nuget.org/packages/ArturRios.Output) `ProcessOutput` / `DataOutput<T>` envelope — with optional stripping of the quotes and periods FluentValidation puts in its default messages.
{{% /blocks/lead %}}
