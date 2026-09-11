---
name: cratis-chronicle-event-specifications
description: Specify event-append behavior with EventScenario from Cratis.Chronicle.Testing — seeding prior events through Given, appending through When or the event log, and asserting on the append result including constraint violations. Use when the behavior under specification is what reaches the event store. Do not use for command handling or for read-model projection behavior.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-chronicle-event-specifications/SKILL.md -->

# Chronicle event specifications

`EventScenario` runs the append path in-process. No Chronicle server, no
database, no network — an empty event log at sequence number zero, with the
application's constraints discovered and active.

## Verified product sources

| Package | Version | Purpose |
| --- | --- | --- |
| `Cratis.Chronicle.Testing` | `16.45.2` | `EventScenario`, its `Given`/`When` builders, the `IAppendResult` `Should*` assertions |
| `Cratis.Chronicle` | `16.45.2` | `IEventSequence.Append`/`AppendMany`, `IAppendResult`, `ConstraintViolation` |

```bash
dotnet add package Cratis.Chronicle.Testing
```

Reverify against the Chronicle repository before claiming support for another
version. Never translate an assertion name from memory.

## Route near misses

- The behavior is a command running through validators, `Provide()` and
  `Handle()`: use `cratis-application-slice-specifications`.
- The behavior is a projection or reducer building a read model: use
  `cratis-chronicle-read-model-specifications`.
- You are deciding what the constraint should *be*: use
  `cratis-chronicle-event-constraints`.
- The specification style itself — folder shape, naming, what not to specify:
  use `cratis-specification-by-example`.

## When you need this

- A constraint must reject a duplicate or conflicting append.
- A batch append (`AppendMany`) must behave as one unit.
- The event log's own behavior is the subject, not the command that reached it.

## When you do not

- **Concurrency violations cannot be provoked here.** `EventScenario` wires a
  no-op concurrency-scope strategy, so `HasConcurrencyViolations` is never true
  inside the scenario however the append is shaped. Specify concurrency against
  the real kernel with an out-of-process integration specification, and do not
  write a scenario specification that looks like it covers it — it would pass
  vacuously, which is worse than not having one.

## Step 1 — Create the scenario

A new `EventScenario` per specification, never shared state.

```csharp
var scenario = new EventScenario();
```

The parameterless constructor uses the default event log, an in-memory event
store, the `default` namespace, and **auto-discovers the constraints** declared
in the loaded assemblies — which is what makes a constraint specification work
without wiring. Pass an `ICanProvideConstraints` to the single-argument
constructor when the specification needs a narrower set.

## Step 2 — Seed prior state through `Given`

```csharp
await scenario.Given
    .ForEventSource(authorId)
    .Events(new AuthorRegistered("Jane Smith"), new BookAdded("Clean Code"));
```

Chain a second `ForEventSource` for a different event source:

```csharp
await scenario.Given.ForEventSource(author1Id).Events(new AuthorRegistered("Jane Smith"));
await scenario.Given.ForEventSource(author2Id).Events(new AuthorRegistered("John Doe"));
```

- `Given` returns a bare `Task` — it carries no result, because seeding is not
  the action under specification.
- Seed **before** the act phase; seeded events take monotonically increasing
  sequence numbers.
- Never put the act under test inside `Given`. Only the world as it already is
  belongs there.

## Step 3 — Append, and hold the result

Two equivalent surfaces. `When` reads as the act phase; the event log is the
direct one.

```csharp
// The act-phase builder — returns Task<AppendResult>
_result = await scenario.When
    .ForEventSource(authorId)
    .Events(new AuthorRegistered("Jane Smith"));

// The event log directly
_result = await scenario.EventLog.Append(authorId, new AuthorRegistered("Jane Smith"));
```

`When…Events(@event, params object[] additionalEvents)` takes the first event
positionally and **stops at the first non-successful append**, so a specification
that appends several events asserts on the first failure rather than the last.

`AppendMany` on the event log appends a batch for one event source:

```csharp
_result = await scenario.EventLog.AppendMany(cartId, [
    new ItemAddedToCart(itemId1),
    new ItemAddedToCart(itemId2)
]);
```

`Append` and `AppendMany` are members of the event sequence reached through
`scenario.EventLog`, not of `EventScenario` itself.

## Step 4 — Assert on the result

Every assertion below is an extension on `IAppendResult` and throws
`AppendResultAssertionException` on failure, with a message listing the
violations and errors it found.

| Method | Asserts |
| --- | --- |
| `ShouldBeSuccessful()` | No violation and no error |
| `ShouldBeFailed()` | Any violation or error |
| `ShouldHaveConstraintViolations()` | At least one constraint violation |
| `ShouldNotHaveConstraintViolations()` | No constraint violation |
| `ShouldHaveConstraintViolationFor(name)` | A violation for the named constraint |
| `ShouldHaveConstraintViolation(name)` | The same assertion, singular spelling |
| `ShouldHaveErrors()` | At least one error |
| `ShouldNotHaveErrors()` | No error |

`ShouldHaveConcurrencyViolations()` and `ShouldNotHaveConcurrencyViolations()`
also exist, but see *When you do not* above — the first can never pass inside a
scenario and the second passes vacuously.

The constraint name parameter is a `ConstraintName`, which converts implicitly
from `string`, so a literal compiles. Prefer a shared constant
(`<Module>ConstraintNames.UniqueX`) so a rename moves both sides at once.
**Assert the name, never the message.**

## Step 5 — Write the specification

Specification files live in the slice folder, wrapped in `#if DEBUG … #endif` so
specification code ships only in Debug.

```csharp
#if DEBUG
namespace MyApp.Library.Authors.when_registering;

public class and_the_name_already_exists : Specification
{
    EventScenario _scenario;
    IAppendResult _result;

    async Task Establish()
    {
        _scenario = new EventScenario();
        await _scenario.Given
            .ForEventSource(AuthorId.New())
            .Events(new AuthorRegistered("Jane Smith"));
    }

    async Task Because() =>
        _result = await _scenario.EventLog.Append(AuthorId.New(), new AuthorRegistered("Jane Smith"));

    [Fact] void should_be_failed() => _result.ShouldBeFailed();
    [Fact] void should_have_constraint_violation_for_unique_name() =>
        _result.ShouldHaveConstraintViolationFor(AuthorConstraintNames.UniqueAuthorName);
}
#endif
```

Use per-specification unique values (`$"{Guid.NewGuid():N}"`) so the suite does
not depend on execution order. For a release, append the removal event and then
assert the value can be claimed again.

## What breaks

- **The duplicate append succeeds.** The constraint was not discovered — the
  scenario was given an explicit constraint provider that excludes it, or the
  constraint type is not in a loaded assembly.
- **`ShouldHaveConstraintViolationFor` fails with an empty violation list on a
  batch append.** `When…Events` short-circuits, so the conflict may be on an
  event the append never reached.
- **A concurrency specification is always green.** It is vacuous — the scenario
  disables the check. Delete it or move it out-of-process.
- **The specification passes alone and fails in the suite.** A hard-coded unique
  value collided with another specification's claim.

## How it is proven

`dotnet build` in Debug (which compiles the `#if DEBUG` specification code) and
`dotnet test`, both clean. A constraint specification is only meaningful if it
has been seen to fail: remove the seeding `Given` once and confirm the
specification goes red before trusting it green.
