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

Generated parameters arrived in Arc **22.66.0**, scenario pinning in **22.67.0**,
and analyzers in **22.68.0**. The examples and behavior here are verified at
**v22.68.0**, which pins Fundamentals **7.23.0**.

Use a concept for a server-owned new identifier. This standalone Arc example
allocates an id; it does not append an event or persist a cart:

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Concepts;

namespace Shopping.Carts;

/// <summary>
/// Represents a cart identifier.
/// </summary>
/// <param name="Value">The underlying identifier.</param>
public record CartId(Guid Value) : ConceptAs<Guid>(Value), IGeneratable<CartId>
{
    /// <summary>
    /// Creates a new cart identifier.
    /// </summary>
    /// <returns>A new cart identifier.</returns>
    public static CartId New() => new(GenerateValue.Uuid());
}

/// <summary>
/// Allocates a server-owned cart identifier.
/// </summary>
[Command]
public record AllocateCartId
{
    /// <summary>
    /// Returns the allocated identifier to the caller.
    /// </summary>
    /// <param name="cartId">The server-generated identifier.</param>
    /// <returns>The allocated identifier.</returns>
    public CartId Handle([GeneratedValue("cart")] CartId cartId) => cartId;
}
```

For a Chronicle stream identity, derive `CartId` from `EventSourceId<Guid>`
instead; see `cratis-fundamentals-concept`. Return `(id, event)` to both append
and give the caller the new stream id, as in SKILL.md. An identifier is not an
event, and generation alone does not persist anything.

Keep existing/client-chosen identifiers and references on command properties.
Services and read models remain ordinary dependencies, not generated values.
UUID generation does not establish authorization, secrecy, idempotency, storage
freshness, or gapless/sequential numbering. Use the domain's authoritative
allocation mechanism for invoice numbers, not a random UUID strategy.

## Resolution, validation and captures

`GeneratedValueAttribute` targets parameters only. Use it on a command's public
instance `Handle()`, not a property, constructor or `Provide()` parameter.
The type must be non-nullable and implement `IGeneratable<TSelf>` for itself;
a raw `Guid`, nullable concept or by-reference parameter is invalid. Use domain
concepts; the runtime's type check is the self-generatable interface contract.

`[GeneratedValue]` uses the parameter name. `[GeneratedValue("cart")]` keeps the
pin/capture name stable across a parameter rename. Names are case-sensitive,
non-whitespace and unique within the command.

Arc first runs `Provide()` and checks its control results. Only if execution may
continue does it call the concept's `New()` strategy and validate the value before
invoking `Handle()`. Neither a provided value nor a DI registration overrides a
generated parameter. Unmarked parameters retain their normal resolution.

Discoverable concept validators run with the command's scoped provider and
cancellation token. Errors produce `GeneratedValueRejected`, an exception
outcome in `CommandResult`: a server-generation defect, not a request-validation
400. Warnings/information allow execution. Fix the strategy or invariant; do not
turn the defect into a user rejection or disable validation to make it pass.

Validated values enter `CommandContext.GeneratedValues` in parameter-resolution
order. Each capture has zero-based `Order`, `Name`, declared `Type` and `Value`;
`Get<CartId>("cart")` retrieves the latest matching capture. Rejected values are
not captured. A Handle parameter is not a request property and is not audited
as command input. Do not log or trace captures or validator failure text, which
can contain values. Returning a value or putting it in an event still follows the
ordinary response, event and compliance contracts.

## Analyzer diagnostics

Arc v22.75.0 reports these at compile time; none has a code fix. Runtime
handler-construction checks remain for declarations compiled without analyzers.

| Diagnostic | Severity | Fix |
| --- | --- | --- |
| ARC0022 | Error | Use a non-nullable type implementing `IGeneratable<TSelf>` for itself; no raw `Guid` or by-reference parameter. |
| ARC0023 | Error | Move the attribute to a non-static command `Handle()` parameter, not `Provide()`, a read model or controller. |
| ARC0024 | Error | Give each generated parameter a non-empty unique name; comparison is case-sensitive. |
| ARC0025 | Warning | Remove a redundant provided value of the generated type, or remove the attribute if `Provide()` should supply it. An unmarked parameter can still consume that provided type. |

## Migrate generation inside Handle

For `var id = new CartId(Guid.NewGuid());` or `var id = CartId.New();` in
`Handle()`, keep the request contract unchanged:

1. Make the concept implement `IGeneratable<CartId>` with static `New()` using
   `GenerateValue.Uuid()` or `GenerateValue.UuidV7()`.
2. Replace the local allocation with `[GeneratedValue("cart")] CartId cartId`
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

- [Generated values](/arc/backend/csharp/commands/generated-values/)
- [Pin generated values in command specs](/arc/backend/csharp/testing/generated-values/)
- [Migrate generated identifiers](/arc/upgrading/generated-values/)
- Analyzer pages: [ARC0022](/arc/backend/csharp/code-analysis/arc0022/),
  [ARC0023](/arc/backend/csharp/code-analysis/arc0023/),
  [ARC0024](/arc/backend/csharp/code-analysis/arc0024/),
  [ARC0025](/arc/backend/csharp/code-analysis/arc0025/).

Source rechecked at **Arc v22.75.0** (generation/pinning APIs unchanged since
the prior verification): `Source/DotNET/Arc.Core/Commands/ModelBound/GeneratedValueAttribute.cs`,
`Commands/GeneratedValueParameters.cs`, `GeneratedValueFactories.cs`,
`CommandHandlerArgumentResolver.cs`, `GeneratedValueResolution.cs`;
`Source/DotNET/Arc.Core.CodeAnalysis/DiagnosticDescriptors.cs`; and
`Source/DotNET/Testing/Commands/CommandScenarioGeneratedValueExtensions.cs`.

This release has no freshness/collision guard, contextual clock/tenant/pattern
generation, or additional built-in strategies beyond Fundamentals' UUID/UUIDv7
helpers at this Arc tag. Screenplay 4.147.0+ separately admits concept `generate`
strategies and freshness; see `cratis-screenplay-command-surface/references/generated-values-and-responses.md`
for that language contract and Stage's explicit scopes, not native Arc freshness support.
