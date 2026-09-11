---
name: cratis-application-slice-specifications
description: Specify the backend behavior of an event-sourced Cratis application slice with the in-process scenario family — CommandScenario, EventScenario, ReadModelScenario and ReactorScenario — including what to cover for each slice type and where the specification files live. Use when adding or changing backend behavior in an application built on Cratis. Do not use for framework library specifications and do not use for frontend behavior.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-application-slice-specifications/SKILL.md -->

# Cratis application slice specifications

A slice is specified from the outside, through the pipeline it actually runs on.
The scenario family exercises the real Arc and Chronicle code paths in-process —
no HTTP, no fixture, no server — so a specification says what the slice does
rather than what its classes do.

This skill decides **which scenario** and **what to cover**. The C# mechanics
(`Establish`/`Because`, substitutes, contexts) live in
`cratis-specifications-csharp`.

## Verified product sources

| Package | Version | Purpose |
| --- | --- | --- |
| `Cratis.Arc.Testing` | `22.10.4` | `CommandScenario<TCommand>` and the `CommandResult` assertions |
| `Cratis.Arc.Chronicle.Testing` | `22.10.4` | `Given`, `EventLog`, `AppendedEvents` and the appended-event assertions on a command scenario |
| `Cratis.Chronicle.Testing` | `16.45.2` | `EventScenario`, `ReadModelScenario<T>`, `ReactorScenario<T>` |
| `Cratis.Specifications.XUnit` | `4.x` | The `Specification` base and the `ShouldXxx` assertions |

`Cratis.Testing` is the meta-package that brings `Cratis.Arc.Testing` and
`Cratis.Arc.Chronicle.Testing` together. Reverify against the owning product
repository before claiming support for another version.

## Route near misses

- The repository builds a Cratis **library** rather than an application: use
  `cratis-specifications-csharp` and stay on the plain `Specification` base.
- React, TypeScript, or a view model: use
  `cratis-application-react-specifications`.
- The subject is append semantics or a constraint on its own: use
  `cratis-chronicle-event-specifications`.
- The subject is a projection or reducer on its own: use
  `cratis-chronicle-read-model-specifications`.
- The question is what the slice *should do*: settle the behavior first. A
  specification records a decision; it does not make one.

## Step 1 — Place the files

Specifications live **in the slice folder**, beside the `.cs` they specify, each
file wrapped in `#if DEBUG … #endif` so specification code compiles only in
Debug.

```
<Feature>/<Slice>/
├── <Slice>.cs
└── when_<verb_phrase>/
    ├── and_<happy_scenario>.cs
    └── and_<failure_scenario>.cs
```

- Folder: `when_<verb_phrase>` — `when_registering`.
- File: `and_<condition>.cs` — `and_name_is_unique.cs`, `and_name_already_exists.cs`.
- Method: `should_<expected_result>`.

A behavior covering several subjects groups under
`<Slice>/for_<Subject>/when_<behavior>/`.

## Step 2 — Pick the scenario by slice type

| Slice type | Scenario | Skill |
| --- | --- | --- |
| State Change — a command appends events | `CommandScenario<TCommand>` | this one |
| Append semantics, a constraint | `EventScenario` | `cratis-chronicle-event-specifications` |
| State View — a projection or reducer | `ReadModelScenario<TReadModel>` | `cratis-chronicle-read-model-specifications` |
| Automation or Translation — a reactor | `ReactorScenario<TReactor>` | `cratis-specifications-csharp` |
| A host, transport, or real-infrastructure boundary | Out-of-process integration | `cratis-specifications-csharp` |

## Step 3 — Cover every outcome of a State Change slice

One specification class for **each** of:

1. **The happy path** — the command succeeds and the expected event is appended.
2. **Each validation failure** — one `and_` class per `CommandValidator` or
   `ConceptValidator` rule.
3. **Each business-rule rejection** — one `and_` class per condition in
   `Handle()` that inspects a read model.
4. **Each constraint violation** — one class per constraint, written with
   `EventScenario`, because a constraint is enforced at the append and not by
   the command.

## Step 4 — `CommandScenario<TCommand>`

It runs authorization, the validators, `Provide()` and `Handle()` in-process and
exposes what was appended.

```csharp
#if DEBUG
namespace MyApp.Projects.Registration.when_registering;

public class and_name_is_unique : Specification
{
    readonly CommandScenario<RegisterProject> _scenario = new();
    readonly ProjectId _id = ProjectId.New();
    CommandResult _result;

    async Task Because() => _result = await _scenario.Execute(new RegisterProject(_id, "My Project"));

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] async Task should_have_appended_the_registered_event() =>
        await _scenario.ShouldHaveAppendedEvent<RegisterProject, ProjectRegistered>(_id, e => e.Name == "My Project");
}
#endif
```

The scenario's own surface is `Services`, `Context`, `Execute(command)` and
`Validate(command)` — `Validate` runs the filters without the handler.
Everything else is an extension method.

## Step 5 — Seed prior state through `Given`

With `Cratis.Arc.Chronicle.Testing` referenced, a command scenario has a
Chronicle-backed `Given` builder, plus `EventScenario`, `EventLog`,
`EventSequence` and `AppendedEvents`.

```csharp
// Seed the events the world already contains. A read model the command injects
// for this event source is materialized from them by its own projection or
// reducer — no read-model type is named.
_scenario.Given.ForEventSource(_cartId).Events(new ItemAddedToCart(itemId));

// Or pin a specific read-model value directly, when the events are beside the point.
_scenario.Given.ForEventSource(_cartId).ReadModel(new CartSummary { ItemCount = 3 });
```

Prefer seeding **events**: it exercises the projection that production relies on,
so a specification cannot pass against a read-model shape the projection never
produces. Pin an instance only when the events would be noise.

Both builder methods return `void`; there is nothing to await.

Validator and `Provide()` dependencies are registered in `_scenario.Services`;
the concrete validator itself is discovered automatically.

## Step 6 — Assert twice on every unhappy path

```csharp
[Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();
[Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();
```

`ShouldNotBeSuccessful()` alone cannot distinguish a validation rejection from an
unhandled exception, so both facts are required. **Never assert on a message
string** — it is presentation text.

`CommandResult` assertions, from `Cratis.Arc.Testing.Commands`, throwing
`CommandResultAssertionException`:

| Assertion | Says |
| --- | --- |
| `ShouldBeSuccessful()` / `ShouldNotBeSuccessful()` | Authorized, valid, no exceptions — or not |
| `ShouldBeValid()` | Validation only; it checks neither authorization nor exceptions |
| `ShouldHaveValidationErrors()` | At least one validation error |
| `ShouldHaveValidationErrorFor(message)` | A validation error with that **message** |
| `ShouldHaveValidationErrorBecauseOf(reason)` | A validation error with that `ValidationResultReason` |
| `ShouldBeAuthorized()` / `ShouldNotBeAuthorized()` | The authorization outcome |
| `ShouldHaveExceptions()` / `ShouldNotHaveExceptions()` | Whether the handler threw |
| `ShouldHaveConstraintViolationFor(name)` | A constraint violation surfaced on the command result |

`ShouldHaveValidationErrorBecauseOf(reason)` is the message-free way to say
*which kind* of rejection happened — `ValidationResultReason.ConstraintViolation`,
`ConcurrencyViolation`, `ValidatorFailed`, `Rule`. Prefer it over
`ShouldHaveValidationErrorFor(message)`, which pins presentation text.

Authorization failures are different in kind: an unauthorized result carries
**no** validation errors, so assert `ShouldNotBeAuthorized()`. Adding an
authorization attribute to an existing command therefore breaks both its
happy-path and its validation-failure specifications — switch the assertions
rather than patching around them.

## Step 7 — Assert what was appended

The appended-event assertions come from `Cratis.Arc.Chronicle.Testing` and are
keyed by **command and event type**. They return `Task`, so the fact is
`async Task`, and they throw `EventSequenceAssertionException`.

- `ShouldHaveAppendedEvent<TCommand, TEvent>(eventSourceId)`
- `ShouldHaveAppendedEvent<TCommand, TEvent>(eventSourceId, Func<TEvent, bool> predicate)`
- `ShouldHaveTailSequenceNumber<TCommand>(sequenceNumber)`

⚠️ On a command scenario the second argument is a **predicate**
(`Func<TEvent, bool>`), not an assertion callback. The `Action<TEvent>` validator
overloads exist only on the `IEventSequence` extensions in
`Cratis.Chronicle.Testing`, which is a different receiver.

Sequence numbers are **zero-based**: the first event is `0`, and the tail after a
single append is `0`, never `1`.

## What breaks

- **A specification passes alone and fails in the suite.** A hard-coded value
  collided with another specification's uniqueness claim. Use a per-specification
  value — a fresh `Guid`, or `Guid.NewGuid().ToString("N")[..9]`. Adding
  `[Collection(…)]` is the wrong fix here.
- **`Given` does not resolve.** `Cratis.Arc.Chronicle.Testing` (or the
  `Cratis.Testing` meta-package) is not referenced. It is an extension member,
  not part of `CommandScenario` itself.
- **A DCB rejection specification is green for the wrong reason.** The read model
  was never seeded, so the handler saw a default or `null` instance and rejected
  on that instead of on the rule. Seed, then confirm the specification fails when
  the seeding is removed.
- **The command scenario reports success but nothing was appended.** The
  assertion is on `CommandResult` only; add a
  `ShouldHaveAppendedEvent<TCommand, TEvent>` fact.
- **Injected-validator branches flip with test ordering.** The command-scenario
  pipeline can cache enough state that a validator branch depends on execution
  order under parallel xUnit runs. Use `CommandScenario` for the valid path, test
  rejected state variants by instantiating the validator directly, and only then
  put that command's specifications in a small `[Collection]`.

## How it is proven

`dotnet build -c Debug` — it compiles the `#if DEBUG` specification code — then
`dotnet test`, both clean, with a specification for the happy path and one per
rejection. Before trusting a rejection specification green, remove the condition
that causes the rejection once and confirm it goes red.
