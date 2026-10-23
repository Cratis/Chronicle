<!-- cratis-ai-managed: skills/cratis-arc-command-validation/references/validation-severity.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Choose which severities block

Model-bound commands accept `X-Allowed-Severity` (`allowedSeverity`). Under
read protection `Off` and `Measure`, a caller sending `3` (Error) removes Error
results from validators, command filters and `Provide()`, so `Handle()` runs
and a validator's state rule can be skipped unless the command declares
`[BlockOnValidationSeverity(ValidationResultSeverity.Error)]`
(`Cratis.Arc.Commands.ModelBound`). Severity is not authorization.

Requires Arc 22.108.0 for the `On` floor: a command declared `On` blocks `Error`
and `Unknown` whatever the caller sends; the caller can relax only `Warning`
and `Information`. Lowering `ReadProtection:MaximumMode` at runtime does not
remove the floor, and an explicit attribute wins. Check with
`dotnet list package`; on older Arc declare the blocking severity explicitly.

Specify both the default refusal and a caller sending a permissive severity.
Assert the command did not write events; a displayed message alone is not proof
that validation blocked execution. Run `dotnet test` on the owning spec project.
