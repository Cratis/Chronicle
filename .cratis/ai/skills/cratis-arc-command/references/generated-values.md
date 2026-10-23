<!-- cratis-ai-managed: skills/cratis-arc-command/references/generated-values.md -->
# Server-generated Handle values

## Contents

- Declare the value and command
- Resolution, validation and captures
- Analyzer diagnostics
- Migrate generation inside Handle
- Pin generated values in specifications
- Verified sources and release boundary

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Declare the value and command

Requires Arc 22.66.0 for generated parameters.

Requires Arc 22.67.0 for scenario pinning.

Requires Arc 22.68.0 for the ARC0022–ARC0025 analyzers.

For the current threaded example, read [shop commands](shop-generation.md) and
[shop concepts](../../cratis-fundamentals-concept/references/shop-generation.md).
Those declarations show:
CustomerId (`cus_` + ULID), context-aware TypeID OrderId, NanoID InviteCode and
secret bytes ApiKey, readable InvoiceNumber (pattern/checksum) and Int64 pickup
TicketNumber (`Integer`). The latter helpers require Fundamentals 7.25.0; inspect
`dotnet list package --include-transitive` before use and retain existing factories
on older packages. They allocate responses, not events or persisted entities.
For a real Chronicle stream identity use `EventSourceId<T>`; return `(id, event)`
to append and return the stream ID, or use an explicit event-source wrapper.

Keep existing/client-chosen identifiers and references on command properties.
Services and read models remain ordinary dependencies, not generated values.
UUID generation does not establish authorization, secrecy, idempotency, storage
freshness, or gapless/sequential numbering. Use the domain's authoritative
allocation mechanism for gapless/sequential invoice numbers, not a random strategy.
A pattern supplies a readable reference, not an allocator or secret; check digits
catch typos, not forgery. The concept validator can call
`GenerateValue.HasValidChecksum(value, Checksum.Luhn)` alongside shape checks;
pinning bypasses generation, never validation. Keep short-code uniqueness separate.

## Resolution, validation and captures

`GeneratedValueAttribute` targets parameters only. Use it on a command's public
instance `Handle()`, not a property, constructor or `Provide()` parameter.
The type must be non-nullable and implement `IGeneratable<TSelf>` or
`IGeneratableInContext<TSelf>` for itself; a raw `Guid`, nullable concept or
by-reference parameter is invalid. Use domain
concepts; the runtime's type check is the self-generatable interface contract.

`[GeneratedValue]` uses the parameter name. `[GeneratedValue("customerId")]` keeps the
pin/capture name stable across a parameter rename. Names are case-sensitive,
non-whitespace and unique within the command.

Arc first runs `Provide()` and checks its control results. Only if execution may
continue does it call the concept's `New()` strategy and validate the value before
invoking `Handle()`. Neither a provided value nor a DI registration overrides a
generated parameter. Unmarked parameters retain their normal resolution.

Discoverable concept validators run with the command's scoped provider and
cancellation token. A failed rule produces `GeneratedValueRejected`, an exception
outcome in `CommandResult`: a server-generation defect, not a request-validation
400. Requires Arc 22.104.0 for rejection at every severity, including Warning and
Information; older runtimes let those severities execute. Fix the strategy or
invariant; do not turn the defect into a user rejection or disable validation.

Validated values enter `CommandContext.GeneratedValues` in parameter-resolution
order. Each capture has zero-based `Order`, `Name`, declared `Type` and `Value`;
`Get<CustomerId>("customerId")` retrieves the latest matching capture for the
shop's CustomerId parameter. Rejected values are
not captured. A Handle parameter is not a request property and is not audited
as command input. Do not log or trace captures or validator failure text, which
can contain values. Returning a value or putting it in an event still follows the
ordinary response, event and compliance contracts.

## Analyzer diagnostics

Arc reports these at compile time; none has a code fix. Runtime
handler-construction checks remain for declarations compiled without analyzers.

| Diagnostic | Severity | Fix |
| --- | --- | --- |
| ARC0022 | Error | Use a non-nullable type implementing `IGeneratable<TSelf>` or `IGeneratableInContext<TSelf>` for itself; no raw `Guid` or by-reference parameter. |
| ARC0023 | Error | Move the attribute to a non-static command `Handle()` parameter, not `Provide()`, a read model or controller. |
| ARC0024 | Error | Give each generated parameter a non-empty unique name; comparison is case-sensitive. |
| ARC0025 | Warning | Remove a redundant provided value of the generated type, or remove the attribute if `Provide()` should supply it. An unmarked parameter can still consume that provided type. |

## Migrate generation inside Handle

For `var id = CustomerId.New();` in `Handle()`, keep the request unchanged:

1. Keep CustomerId's `IGeneratable<CustomerId>` strategy (`cus_` + ULID), or
   OrderId's context contract, as shown in [shop generation](shop-generation.md).
2. Replace local allocation with `[GeneratedValue("customerId")] CustomerId customerId`
   on `Handle()`, then use that parameter in the existing return/operation shape.
3. Return the id if the caller needs it; a capture is not an automatic response.
4. Pin the concept by generated name in pipeline specifications. For a pure
   direct-call specification, pass the known concept to `Handle()` yourself.

Do not migrate caller-owned inputs into server generation. A retried request
would otherwise allocate a different value; random generation is not an
idempotency mechanism. UUIDv7 reveals creation time and is not monotonic within
a millisecond; choose it for index locality only after checking storage ordering.

## Pin generated values in specifications

Use `cratis-specifications-csharp` and its `references/application-scenarios.md`
for the complete pinned example. `Cratis.Arc.Testing.Commands` supplies
`PinGeneratedValue(name, concept)`, `AllowGeneration()` and the `GeneratedValues`
capture helpers on `CommandScenario<TCommand>`.

Scenarios default to strict: an unpinned generated parameter yields
`GeneratedValueNotPinned` in the command result, not a throw from `Execute`.
Configure before execution. Nested pins use both generic arguments at the call
site: `PinGeneratedValue<TCommand, TOtherCommand>(name, concept)`.

## Verified sources and release boundary

Published guides:

- [Generated values](https://www.cratis.io/arc/backend/csharp/commands/generated-values/)
- [Pin generated values in command specs](https://www.cratis.io/arc/backend/csharp/testing/generated-values/)
- [Migrate generated identifiers](https://www.cratis.io/arc/upgrading/generated-values/)
- Analyzer pages: [ARC0022](https://www.cratis.io/arc/backend/csharp/code-analysis/arc0022/),
  [ARC0023](https://www.cratis.io/arc/backend/csharp/code-analysis/arc0023/),
  [ARC0024](https://www.cratis.io/arc/backend/csharp/code-analysis/arc0024/),
  [ARC0025](https://www.cratis.io/arc/backend/csharp/code-analysis/arc0025/).

Source: `Source/DotNET/Arc.Core/Commands/ModelBound/GeneratedValueAttribute.cs`,
`Commands/GeneratedValueParameters.cs`, `GeneratedValueFactories.cs`,
`CommandHandlerArgumentResolver.cs`, `GeneratedValueResolution.cs`;
`Source/DotNET/Arc.Core.CodeAnalysis/DiagnosticDescriptors.cs`; and
`Source/DotNET/Testing/Commands/CommandScenarioGeneratedValueExtensions.cs`.

Contextual generation (received time and tenant) and Fundamentals' additional strategies need a newer Arc; [sources and Clock/Tenant
setup](shop-generation.md). Generated parameter resolution is not a gapless or
short-code collision allocator. Fundamentals owns pattern/checksum factories;
Arc resolves these through the concept's generation interface, not strategy-specific
`[GeneratedValue]` options. Requires Arc.Screenplay 22.107.0 for pattern/random,
checksum and self-contained String-rule recovery; older adapters keep them in code.
Requires Stage 4.118.0 for checksum validation and 4.119.0 for pattern/random
rendering; older renderers refuse them. Requires Stage 4.136.0 to render `generate rule` as an
`IGeneratableInContext<T>.New(GenerationContext)` factory (body errors STAGE-ESM-019, shadowing -022, missing
attachment -020); the Host and legacy syntax engine still refuse a command generating its value with STAGE-ESM-028.
Screenplay separately admits concept `generate` strategies and freshness; see `cratis-screenplay-command-surface/references/generated-values-and-responses.md`
for that language contract and Stage's explicit scopes. Native Arc freshness is a
separate policy; a factory alone is not a storage guard.
