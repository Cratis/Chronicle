---
name: cratis-arc-command
description: Define a Cratis Arc command — the [Command] record, its Handle() method and return shape, the optional Provide() step, and the generated TypeScript proxy. Use when adding a command, choosing what Handle() should return, deciding which values may reach the causation chain (including API keys and other credentials passed to a command, which need [NotAudited] and, when retained, [Encrypted]), or wiring a form or button to an Arc backend. Do not use for a validation-only change or merely to execute an existing command.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-arc-command/SKILL.md -->

# Define an Arc command

A command is a record that carries the user's intent and owns its own handler.
Arc discovers it, runs authorization and validation, calls `Handle()`, and turns
whatever `Handle()` returns into events, server-executed operations, a response,
or a combination.

- An accepted `.play` model under the model root covers the behavior, or the
  repository is opted in (per `general.md` "Model first"; where `cratis-screenplay-modeling-lifecycle` is installed, run its `scripts/model-first-status.mjs`): change the model first with
  `cratis-screenplay-event-modeling`. If the Screenplay skills are not installed,
  say so and do not author `.play` from memory.
  Edit code here only for infrastructure, clients, adapters, Screenplay code
  attachments, or gap-fill scope (`cratis-screenplay-render-and-gap-fill`);
  never edit Stage-managed output.

## Verified product sources

Read [references/product-sources.md](references/product-sources.md) for the owning packages and `Result`.

Arc without Chronicle is a
supported setup; everything on this page that appends events needs Chronicle.
See `cratis-arc-command-operation` for the operation source and recovery contract.

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
- For returned operations, `ARC0016` checks method shapes, `ARC0017` rejects bare
  operation collections, and `ARC0018` checks generated-invoker accessibility.
- Name the command as the action — `OpenDebitAccount`, not
  `OpenDebitAccountCommand`.
- Unmarked `Handle` parameters resolve from `Provide()` or DI; ask for services
  there rather than through a constructor. Server-owned new values use
  `[GeneratedValue]` instead (below).
- Never inject `IEventLog` into `Handle` to append the command's own events —
  `ARCCHR0007` warns, and it bypasses Arc's append pipeline. Express the append
  through the return type.

Use concept types rather than raw primitives for the command's values, and give
Chronicle-backed identities the `EventSourceId<T>` base — see the
`cratis-fundamentals-concept` skill for the exact shapes.

## Choose the return shape

`Handle()`'s return value is dispatched by its runtime type. The shape decides
what is appended and what the caller gets back. Operations are the exception:
the *declared* return type must name `ICommandOperation`/`CommandOperations`
(directly or in a tuple, `Task`, `Result`/`OneOf`) for Arc to execute them.

| Return | What Arc does |
| --- | --- |
| A registered event record | Appends it to the command's event source id |
| `IEnumerable<object>` of registered events | Appends each to the command's event source id |
| `EventForEventSourceId(id, @event)` | Appends that one event to `id` |
| A collection mixing events and `EventForEventSourceId` | Appends each to its own target, wrappers to their id and plain events to the command's |
| `ICommandOperation` (or a concrete operation) | Executes server-side after returned events are enrolled; never the response |
| `CommandOperations` | Executes an explicit batch sequentially after event enrollment; never the response |
| A tuple | Each element is dispatched on its own; the one element nothing can handle becomes the response |
| Anything else | Becomes the response payload |
| `Result<TEvent, ValidationResult>` | Success appends the event; failure becomes a validation failure |
| `Task<T>` / `ValueTask<T>` of any of the above | Awaited first, then dispatched |

When choosing a return shape or diagnosing dispatch failures, read [handler shapes](references/handler-shapes.md) for the dispatch order, the
tuple rule, and the exact failure modes.

⚠️ **An unregistered event type is not an error.** No handler claims it, so it
silently becomes the HTTP response body instead of being appended. A command
that "runs fine" but appends nothing is almost always a missing `[EventType]`.

For immediate inline side effects chosen by the command, prefer returning an
operation rather than calling the service inside `Handle()`. Arc executes the
work and selects optional compensation from the commit facts. Direct service
calls remain supported when their result is needed for the decision; durable
after-commit work belongs in reactors/outboxes/workflows. Use
`cratis-arc-command-operation` rather than a custom response handler or rollback
stack for this application work.

**Never return a nullable event or `null` to mean "nothing to do".** Arc only
processes a non-null result, so a `null` is success with nothing appended and the
caller is told a command succeeded that never happened. Missing state or a failed
rule is a rejection: return `Result<TEvent, ValidationResult>` with
`ValidationResult.Error(...)`, or reject in a validator or `Provide()`. Optional
`ICommandOperation` returns are a separate contract and unaffected. `ARCCHR0015` warns on nullable event returns. A nullable
read-model parameter (`ARC0006`) is handled by rejecting, never by returning a
nullable event.

Two consequences worth knowing before writing the first command:

- **The tuple is how a create command returns its new id.** An
  `EventSourceId`-derived value in the tuple is not appendable, so it becomes the
  response — and the response, when it is an event-source id value, is also what
  the events in that same tuple are appended to. Returning more than one
  unhandleable element throws `MultipleUnhandledTupleValues`. When the command
  also returns an operation keyed by that id, have the caller supply the id
  instead of generating a new value, so a retried request reuses the same
  ownership key. A generated value alone does not provide idempotency.
- **Events never carry their own event source.** Cross-stream writes use
  `EventForEventSourceId`; they are not expressed by a property on the event.

Read [references/return-shape-examples.md](references/return-shape-examples.md) for create and cross-stream examples.

## Generate a server-owned new identifier

Requires Arc 22.66.0 for `[GeneratedValue]`.

Replace local `New()` with a `[GeneratedValue]` Handle parameter,
never a request property. Use a non-nullable self-generatable concept and a unique,
case-sensitive generated name. Return the value explicitly if the caller needs it.
Read [generated values](references/generated-values.md) for Provide/DI boundaries,
validation/captures, ARC0022–ARC0025, migration and named pins; context
factories are covered in the shop reference below.

## Choosing a strategy

Use the [threaded shop example](references/shop-generation.md): CustomerId
(`cus_` + ULID), context-aware TypeID OrderId, NanoID InviteCode and secret
`Bytes(32)` ApiKey. Context factories receive captured
received time/tenant (Requires Arc 22.83.0); Clock/Tenant scenario setup
is shown there.

Read references/fresh-identifiers.md when a generated value is an append target:
Requires Arc 22.95.0 for whole-source emptiness protection, collision outcomes,
explicit opt-out and decision-mode scenario setup.

## When not to generate

Read [references/generation-boundaries.md](references/generation-boundaries.md) when deciding whether to generate.

## Where the event source id comes from

Chronicle resolves the command's event source id in this order:

1. `commandContext.Response`, when the response is an event-source-id value —
   the tuple case above;
2. `ICanProvideEventSourceId.GetEventSourceId()` on the command;
3. a single `EventSourceId`/`EventSourceId<T>`-derived or `[Key]`-marked
   property on the command;
4. otherwise a freshly generated id.

Two or more candidates raise `ARCCHR0002` (`[Key]` does not silence it); Arc
silently takes the first declared. Implement `ICanProvideEventSourceId`.

Read [decisions and routing](references/decisions-and-routing.md) when a command
needs guarded state, stream-id templates, definition-backed routes, event tags or
post-commit `CompleteStream`. A read-model parameter guards the append only under
read protection `On`; under `Off` (the default) it is advisory. `Result<,>` and
`concurrency: true` are not protection; see
[choosing how a rule holds](../cratis-chronicle-decision-reads/references/choosing-how-a-rule-holds.md).
A returned `EventForEventSourceId` route wins over command context.

Read [references/event-source-id-details.md](references/event-source-id-details.md) when marking `[Key]` (two attributes share the name; `ARCCHR0008`) or using `[EventSource<TSource>]`.

## Fetch what the handler needs with `Provide()`

`Provide` is an optional instance method that runs before `Handle`. Its purpose
is to keep IO out of `Handle`: it fetches or computes values, and Arc binds them
to `Handle`'s parameters by type.

Read [references/provide-example.md](references/provide-example.md) for a worked `Provide()` example.

- Short-circuit with `ValidationResult.Error(...)`; do not throw for a rejection
  the user can act on. A thrown exception surfaces as `HasExceptions` and HTTP
  500, not as a validation failure.
- Every value `Provide` returns must be consumed by a `Handle` parameter.
  `ARC0005` warns otherwise. `ValidationResult`, `AuthorizationResult` and
  `CommandResult` are exempt because they short-circuit rather than feed `Handle`.
- `Provide` runs after authorization and validation.

An Arc read model can also be injected directly into `Provide`, `Handle`, or a
`CommandValidator<T>` — but only by **the command's own resolved key**, as a plain
read that guards nothing. Reading a
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
| `[Encrypted]` (Chronicle, `Cratis.Chronicle.ProtectedValues`) | an operational secret the system retains — a third-party API key, a company's bank account number | encrypted at rest on the event, no erasure; does not withhold it from causation |
| `[NotAudited]` (Arc Chronicle) | the same operational secret, and any credential given as a command input | withheld from causation, nothing else |

A retained operational secret needs **both** `[Encrypted]` and `[NotAudited]`; either alone leaves a gap. A credential used for authentication (password, token) is marked `[NotAudited]` on the command and reaches an event only as a hash or a reference, never encrypted. Never combine `[PII]` with `[Encrypted]` (`CHR0053`).

Read [references/causation-details.md](references/causation-details.md) when marking an input `[NotAudited]` or reading `ARCCHR0009`.

## Generate the TypeScript proxy

```xml
<PackageReference Include="Cratis.Arc.ProxyGenerator.Build" Version="22.75.0" />

<PropertyGroup>
  <CratisProxiesOutputPath>$(MSBuildThisFileDirectory)../<Web>/src/api</CratisProxiesOutputPath>
</PropertyGroup>
```

`dotnet build` runs the generator after the build, and only when
`CratisProxiesOutputPath` is set. Output folders mirror the C# namespace, not the
file path. When configuring proxy generation or diagnosing missing or stale output, read [proxy generation](references/proxy-generation.md) for the full
set of MSBuild knobs and the common failures.

Read [references/proxy-generation.md](references/proxy-generation.md#blocking-severity) when a command declares `[BlockOnValidationSeverity]`.

## The generated client contract

The generated proxy is a `Command` from `@cratis/arc`. Its members are the same
whatever renders it:

Read [references/client-contract.md](references/client-contract.md) for the generated proxy members.

Read `references/command-result.md` when branching on a command result, matching validation failures to fields, or mapping integration exceptions. Branch on the specific flag, not only on `isSuccess`. The exact `CommandResult`
and `ValidationResult` shapes are in
[command result](references/command-result.md) — in particular, a validation
failure carries `members: string[]` (camelCased) and a numeric `severity`, not a
`propertyName` string.

`validateClientSide()` runs only the rules the generator could extract, so it can
pass where `execute()` still fails validation — read
[proxy generation](references/proxy-generation.md) when checking which validation rules are extractable.

Binding this proxy into a React component — the generated `use()` hook and the
Cratis Components command dialog and form fields — belongs to the Arc React and
Components guidance, not to this skill.

## Route near misses

- Adding or changing a rule on an existing command: the Arc command validation
  guidance.
- Executing an existing command from backend code: `cratis-arc-command-execution`.
- Declaring inline work, compensation, or operation specs:
  `cratis-arc-command-operation`.
- Append-time uniqueness or concurrency constraints: the Chronicle event
  constraints guidance.
- Choosing the concept or identity type for a value: `cratis-fundamentals-concept`.

## Verify

- The command is a `record`, carries `[Command]`, and declares a public instance
  `Handle`.
- The return shape matches what the command is supposed to do, and any
  cross-stream event is wrapped in `EventForEventSourceId`.
- At most one tuple element is unhandleable.
- Inline work follows `cratis-arc-command-operation`: explicit operation return
  shapes, no duplicate direct write, and tested execution/recovery boundaries.
- The event source id resolves from exactly one place; `ARCCHR0002` is silent.
- `[Key]`, where used, is `Cratis.Chronicle.Keys.KeyAttribute`.
- No `IEventLog` is injected into `Handle`.
- Every value `Provide` returns is consumed by an unmarked `Handle` parameter.
- Generated parameters follow [generated values](references/generated-values.md)
  and are pinned before scenario execution (`cratis-specifications-csharp`).
- User-correctable rejections are validation results, never thrown exceptions;
  `GeneratedValueRejected` instead signals a defective server generation strategy.
- Every secret or personal value is marked before it can reach the event log.
- `dotnet build` is clean in Debug and Release, with no suppressed `ARC*` or
  `ARCCHR*` diagnostic left unjustified.
- The generated proxy exists at the configured output path and the frontend
  compiles against it.
