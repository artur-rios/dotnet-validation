# Changelog

All notable changes to `ArturRios.Validation` are recorded in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Fixed

- `removeSpecialChars` strips only the quoting apostrophes and the sentence-ending full stops FluentValidation puts
  in its messages. It used to remove every `'` and `.`, which changed what a message said: `'Amount' must be greater
  than '0.5'.` came out as `Amount must be greater than 05`. An apostrophe between two letters (`can't`) and a full
  stop followed by anything but whitespace (`0.5`, `example.org`) are now kept; FluentValidation's default messages
  come out exactly as before.
- A failure whose message is blank, or blank once `removeSpecialChars` has run, is reported as
  `The specified condition was not met for '<property>'.` instead of being dropped by `ProcessOutput.AddErrors` —
  which left `ValidateAndReturnProcessOutput` and `ValidateAndReturnDataOutput` (and their asynchronous
  counterparts) reporting `Success` for an invalid model.

## [2.0.0] - 2026-08-24

### Added

- `ValidateAndReturnErrorsAsync`, `ValidateAndReturnProcessOutputAsync` and `ValidateAndReturnDataOutputAsync`,
  taking a `CancellationToken`, so a validator declaring an asynchronous rule (`MustAsync`, `CustomAsync`) can use
  the helpers.
- XML documentation for every public type, method and parameter; the package used to ship an empty documentation
  file.
- The NuGet listing carries the project URL and package tags.

### Changed

- **Breaking:** `IFluentValidator<T>` gains `ValidateAndReturnProcessOutput`, `ValidateAndReturnErrorsAsync` and
  `ValidateAndReturnProcessOutputAsync`. Anyone implementing the interface directly has to add them; anyone deriving
  from `FluentValidator<T>` needs no change.
- The special-character regex used by `removeSpecialChars` is source-generated, with a 100 ms match timeout.
- `ArturRios.Output` updated from 3.1.0 to 3.2.0.

## [1.1.0] - 2026-08-19

### Changed

- `ArturRios.Output` updated from 2.0.1 to 3.1.0.

## [1.0.0] - 2026-07-17

### Added

- `FluentValidator<T>`, a FluentValidation `AbstractValidator<T>` with helpers that return the validation errors as
  a string array, a `ProcessOutput` or a `DataOutput<T>`, optionally stripping quotes and periods from the messages.
- `IFluentValidator<T>`, the abstraction over it.

[Unreleased]: https://github.com/artur-rios/dotnet-validation/compare/2.0.0...HEAD
[2.0.0]: https://github.com/artur-rios/dotnet-validation/compare/v1.1.0...2.0.0
[1.1.0]: https://github.com/artur-rios/dotnet-validation/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/artur-rios/dotnet-validation/releases/tag/v1.0.0
