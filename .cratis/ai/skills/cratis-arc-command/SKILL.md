---
name: cratis-arc-command
description: Define a Cratis Arc command — the [Command] record, its Handle() method and return shape, the optional Provide() step, and the generated TypeScript proxy. Use when adding a command, choosing what Handle() should return, deciding which values may reach the causation chain, or wiring a form or button to an Arc backend. Do not use for a validation-only change or merely to execute an existing command.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-arc-command/SKILL.md -->

# Define an Arc command

A command is a record that carries the user's intent and owns its own handler.
Arc discovers it, runs authorization and validation, calls `Handle()`, and turns
whatever `Handle()` returns into appended events, a response, or both.

## Verified product sources

| Package | Version | Purpose |
| --- | --- | --- |
| `Cratis.Arc.Core` | `22.10.4` | `Cratis.Arc.Commands.ModelBound.CommandAttribute`, `CommandResult`, `CommandValidator<T>` |
| `Cratis.Arc.Chronicle` | `22.10.4` | event append handling, `Cratis.Arc.Chronicle.Commands.NotAuditedAttribute` |
| `Cratis.Arc.ProxyGenerator.Build` | `22.10.4` | `CratisProxiesOutputPath` MSBuild integration |
| `Cratis.Fundamentals` | `7.18.2` | `Cratis.Monads.Result<TResult, TError>` |
| `Cratis.Chronicle` | `16.39.1` | `EventTypeAttribute`, `EventForEventSourceId`, `ICanProvideEventSourceId` |
| `@cratis/arc` | `22.10.4` | `ICommand`, `CommandResult`, `ValidationResult` |

Reverify before claiming support for another version. Arc without Chronicle is a
supported setup; everything on this page that appends events needs Chronicle.

## Declare the command

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

namespace <NamespaceRoot>.<Feature>;

[Command]
public record <ImperativeName>(<ConceptType> <Name>, <ConceptType> <Name>)
{
    public <EventType> Handle() => new(<Name>);
}

[EventType]
public record <EventType>(<ConceptType> <Name>);
```

Rules the framework actually enforces:

- `[Command]` is `Cratis.Arc.Commands.ModelBound.CommandAttribute` and applies to
  a class declaration only. A type counts as a command when it carries
  `[Command]` **and** declares a `Handle` method; either one alone is not enough.
- `Handle` is found by name on the command type itself. `ARC0003` is an error
  when another class defines `Handle` for a command, and `ARC0004` is an error
  when a `[Command]` type has no public instance `Handle`.
- `ARC0002` warns when a type has `Handle` but no `[Command]`; `ARC0007` warns
  when a command is a `class` rather than a `record`.
- Name the command as the action — `OpenDebitAccount`, not
  `OpenDebitAccountCommand`.
- `Handle` parameters are resolved from DI, so ask for services there rather
  than through a constructor.
- Never inject `IEventLog` into `Handle` to append the command's own events —
  `ARCCHR0007` warns, and it bypasses Arc's append pipeline. Express the append
  through the return type.

Use concept types rather than raw primitives for the command's values, and give
Chronicle-backed identities the `EventSourceId<T>` base — see the
`cratis-fundamentals-concept` skill for the exact shapes.

## Choose the return shape

`Handle()`'s return value is dispatched by its runtime type. The shape decides
what is appended and what the caller gets back.

| Return | What Arc does |
| --- | --- |
| A registered event record | Appends it to the command's event source id |
| `IEnumerable<object>` of registered events | Appends each to the command's event source id |
| `EventForEventSourceId(id, @event)` | Appends that one event to `id` |
| A collection mixing events and `EventForEventSourceId` | Appends each to its own target, wrappers to their id and plain events to the command's |
| A tuple | Each element is dispatched on its own; the one element nothing can handle becomes the response |
| Anything else | Becomes the response payload |
| `Result<TEvent, ValidationResult>` | Success appends the event; failure becomes a validation failure |
| `Task<T>` / `ValueTask<T>` of any of the above | Awaited first, then dispatched |

See [handler shapes](references/handler-shapes.md) for the dispatch order, the
tuple rule, and the exact failure modes.

Two consequences worth knowing before writing the first command:

- **The tuple is how a create command returns its new id.** An
  `EventSourceId`-derived value in the tuple is not appendable, so it becomes the
  response — and the response, when it is an event-source id value, is also what
  the events in that same tuple are appended to. Returning more than one
  unhandleable element throws `MultipleUnhandledTupleValues`.
- **Events never carry their own event source.** Cross-stream writes use
  `EventForEventSourceId`; they are not expressed by a property on the event.

```csharp
[Command]
public record Register<Thing>(<ConceptType> <Name>)
{
    public (<ThingId>, <ThingRegistered>) Handle()
    {
        var id = <ThingId>.New();
        return (id, new(<Name>));
    }
}
```

```csharp
[Command]
public record Transfer(<AccountId> From, <AccountId> To, <Money> Amount)
{
    public IEnumerable<object> Handle() =>
    [
        new EventForEventSourceId(From, new <Withdrawn>(Amount)),
        new EventForEventSourceId(To, new <Deposited>(Amount)),
    ];
}
```

## Where the event source id comes from

Chronicle resolves the command's event source id in this order:

1. `commandContext.Response`, when the response is an event-source-id value —
   the tuple case above;
2. `ICanProvideEventSourceId.GetEventSourceId()` on the command;
3. a single `EventSourceId`/`EventSourceId<T>`-derived or `[Key]`-marked
   property on the command;
4. otherwise a freshly generated id.

A command with more than one candidate property is ambiguous and `ARCCHR0002`
warns; implement `ICanProvideEventSourceId` to say which value wins. Do not rely
on property order.

Two attributes are spelled `[Key]`. Chronicle reads `Cratis.Chronicle.Keys.KeyAttribute`.
Marking `System.ComponentModel.DataAnnotations.KeyAttribute` in an application
that uses Chronicle compiles, resolves nothing, and silently invents a new event
source id for every command — `ARCCHR0008` reports it.

## Fetch what the handler needs with `Provide()`

`Provide` is an optional instance method that runs before `Handle`. Its purpose
is to keep IO out of `Handle`: it fetches or computes values, and Arc binds them
to `Handle`'s parameters by type.

```csharp
[Command]
public record Open<Thing>(<ThingId> Id, <OwnerId> OwnerId)
{
    public async Task<Result<<Owner>, ValidationResult>> Provide(IReadModels readModels)
    {
        var owner = await readModels.GetInstanceById<<Owner>>((EventSourceId)OwnerId);
        return owner is null
            ? ValidationResult.Error("Owner must exist.")
            : owner;
    }

    public <ThingOpened> Handle(<Owner> owner) => new(owner.Id);
}
```

- Short-circuit with `ValidationResult.Error(...)`; do not throw for a rejection
  the user can act on. A thrown exception surfaces as `HasExceptions` and HTTP
  500, not as a validation failure.
- Every value `Provide` returns must be consumed by a `Handle` parameter.
  `ARC0005` warns otherwise. `ValidationResult`, `AuthorizationResult` and
  `CommandResult` are exempt because they short-circuit rather than feed `Handle`.
- `Provide` runs after authorization and validation.

An Arc read model can also be injected directly into `Provide`, `Handle`, or a
`CommandValidator<T>` — but only by **the command's own resolved key**. Reading a
read model keyed by anything else needs an explicit by-id read, and an absent
instance does not always arrive as `null`. Read
[read-model injection](references/read-model-injection.md) before relying on a
directly injected read model.

## Decide what the causation chain may record

Every property value of the command is written to the causation of every event
the command appends, and the event log is immutable. Decide this when the
property is added.

| Marking | Use for | Effect |
| --- | --- | --- |
| `[PII]` (Chronicle) | personal data | encrypted in the event and enrolled in erasure; already withheld from causation |
| `[NotAudited]` (Arc Chronicle) | a secret that is not personal data — password, token, API key | withheld from causation, nothing else |

```csharp
[Command]
public record Change<Secret>(
    <UserId> User,
    [property: NotAudited] string Old<Secret>,
    [property: NotAudited] string New<Secret>)
{
    public <SecretChanged> Handle(I<Hasher> hasher) => new(hasher.Hash(New<Secret>));
}
```

`[NotAudited]` applies to a class, struct, property, or parameter. On the type it
withholds every property at once. Marking the concept type instead makes it
travel to every command that takes that value. The command is still named on the
chain either way; only the values are withheld.

`ARCCHR0009` warns when a property's *name* reads like a secret and is unmarked.
It cannot see a secret whose name does not say so, so a clean build means
"nothing obvious was missed", not "no secrets are recorded". When the name only
reads like a secret and the value is safe to record, the framework's own guidance
is to mark it `[NotAudited]` anyway or rename the property — the value is written
either way, so the reading is all a reviewer has to go on.

## Generate the TypeScript proxy

```xml
<PackageReference Include="Cratis.Arc.ProxyGenerator.Build" Version="22.10.4" />

<PropertyGroup>
  <CratisProxiesOutputPath>$(MSBuildThisFileDirectory)../<Web>/src/api</CratisProxiesOutputPath>
</PropertyGroup>
```

`dotnet build` runs the generator after the build, and only when
`CratisProxiesOutputPath` is set. Output folders mirror the C# namespace, not the
file path. See [proxy generation](references/proxy-generation.md) for the full
set of MSBuild knobs and the common failures.

## The generated client contract

The generated proxy is a `Command` from `@cratis/arc`. Its members are the same
whatever renders it:

| Member | What it does |
| --- | --- |
| `route` | The generated route, including the configured API prefix |
| `roles` | The roles the command declares; empty when it declares none |
| `<property>` | Get or set one value; setting raises `propertyChanged` |
| `hasChanges` | True when any value differs from the initial values |
| `execute(allowedSeverity?, ignoreWarnings?)` | Sends the command; resolves to `CommandResult` |
| `validate()` | Runs authorization and validation on the server without the handler |
| `validateClientSide()` | Runs only the extracted rules locally; never touches the network |
| `setInitialValues(values)` | Sets the change-tracking baseline |
| `setInitialValuesFromCurrentValues()` | Rebaselines onto the current values |
| `revertChanges()` / `clear()` | Restore the baseline / reset everything |

Branch on the specific flag, not only on `isSuccess`. The exact `CommandResult`
and `ValidationResult` shapes are in
[command result](references/command-result.md) — in particular, a validation
failure carries `members: string[]` (camelCased) and a numeric `severity`, not a
`propertyName` string.

`validateClientSide()` runs only the rules the generator could extract, so it can
pass where `execute()` still fails validation — see
[proxy generation](references/proxy-generation.md) for the exact extractable set.

Binding this proxy into a React component — the generated `use()` hook and the
Cratis Components command dialog and form fields — belongs to the Arc React and
Components guidance, not to this skill.

## Route near misses

- Adding or changing a rule on an existing command: the Arc command validation
  guidance.
- Executing an existing command from backend code: `cratis-arc-command-execution`.
- Append-time uniqueness or concurrency constraints: the Chronicle event
  constraints guidance.
- Choosing the concept or identity type for a value: `cratis-fundamentals-concept`.

## Verify

- The command is a `record`, carries `[Command]`, and declares a public instance
  `Handle`.
- The return shape matches what the command is supposed to do, and any
  cross-stream event is wrapped in `EventForEventSourceId`.
- At most one tuple element is unhandleable.
- The event source id resolves from exactly one place; `ARCCHR0002` is silent.
- `[Key]`, where used, is `Cratis.Chronicle.Keys.KeyAttribute`.
- No `IEventLog` is injected into `Handle`.
- Every value `Provide` returns is consumed by `Handle`.
- Rejections are validation results, never thrown exceptions.
- Every secret or personal value is marked before it can reach the event log.
- `dotnet build` is clean in Debug and Release, with no suppressed `ARC*` or
  `ARCCHR*` diagnostic left unjustified.
- The generated proxy exists at the configured output path and the frontend
  compiles against it.
