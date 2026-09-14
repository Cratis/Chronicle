<!-- cratis-ai-managed: skills/cratis-specifications-csharp/references/application-scenarios.md -->
# In-process scenario specifications

The four scenario helpers exercise the real Arc and Chronicle pipelines
in-process. They are the default for event-sourced *application* behavior. A
library or framework repository reaches for one only to test the very engine it
provides.

Verified against `Cratis.Arc.Testing` `22.10.4` and `Cratis.Chronicle.Testing`
`17.0.0`.

| Helper | Exercises | Use for |
| --- | --- | --- |
| `CommandScenario<TCommand>` | Authorization, validators, `Provide()`, `Handle()`, appended events | State Change behavior |
| `EventScenario` | Chronicle append semantics without the command pipeline | Constraints, raw sequencing, concurrency |
| `ReadModelScenario<TReadModel>` | Projection or reducer state from a sequence of events | State View behavior |
| `ReactorScenario<TReactor>` | Reactor invocation and its side effects | Automation and Translation behavior |

Wrap every scenario specification file in `#if DEBUG … #endif` so specification
code compiles only in Debug.

## `CommandScenario<TCommand>`

```csharp
#if DEBUG
namespace <RootNamespace>.<Feature>.when_<behavior>;

public class and_<condition> : Specification
{
    readonly CommandScenario<<CommandType>> _scenario = new();
    readonly <IdentityType> _id = <IdentityType>.New();
    CommandResult _result;

    async Task Because() =>
        _result = await _scenario.Execute(new <CommandType>(_id, <arguments>));

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();

    [Fact] async Task should_append_<event>() =>
        await _scenario.ShouldHaveAppendedEvent<<CommandType>, <EventType>>(
            _id, appended => appended.<Property> == <expected>);
}
#endif
```

`CommandScenario<TCommand>` itself exposes exactly `Services`, `Context`,
`Execute`, and `Validate`. Everything else — seeding, the event log, and the
assertions — arrives as extension members from `Cratis.Arc.Chronicle.Testing`.

- **Event assertions** are extension methods keyed by command *and* event type:
  `ShouldHaveAppendedEvent<TCommand, TEvent>(eventSourceId)`, its
  `(eventSourceId, Func<TEvent, bool> predicate)` overload, and
  `ShouldHaveTailSequenceNumber<TCommand>(…)`. They return `Task`, so the fact is
  `async Task`.
- **`CommandResult` assertions** come from `Cratis.Arc.Testing.Commands` and
  throw `CommandResultAssertionException` on failure: `ShouldBeSuccessful()`
  (authorized, valid, no exceptions), `ShouldNotBeSuccessful()`,
  `ShouldBeValid()` (validation only — it does not check authorization or
  exceptions), `ShouldHaveValidationErrors()`,
  `ShouldHaveValidationErrorFor(message)`, `ShouldBeAuthorized()`,
  `ShouldNotBeAuthorized()`, `ShouldHaveExceptions()`,
  `ShouldNotHaveExceptions()`.
- **Seed prior state through `Given`.** With `Cratis.Arc.Chronicle.Testing`
  referenced, a command scenario carries a Chronicle-backed `Given` builder as an
  extension member, alongside `EventScenario`, `EventLog`, `EventSequence` and
  `AppendedEvents`. `Given.ForEventSource(id).Events(…)` seeds the events an
  injected read model is then materialized from by its own projection or
  reducer; `Given.ForEventSource(id).ReadModel(instance)` pins a specific
  read-model value instead. Both return `void`. Prefer seeding events — it
  exercises the projection production depends on, so the specification cannot
  pass against a read-model shape the projection never produces.
- **Validator and `Provide()` dependencies** are registered in
  `_scenario.Services`; the concrete validator is discovered automatically.

### Unhappy paths assert twice

```csharp
[Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();
[Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();
```

`ShouldNotBeSuccessful()` alone cannot distinguish a validation rejection from
an unhandled exception, so both facts are required. Authorization failures are
different: an unauthorized result carries **no** validation errors, so assert
`ShouldNotBeAuthorized()` instead. Adding an authorization attribute to an
existing command therefore breaks both its happy-path and its validation-failure
specifications — switch those assertions rather than patching around them.

Never assert on a message string.

> **Validator state can be order-sensitive.** The command-scenario pipeline can
> cache enough state that injected-validator branches depend on execution order
> when xUnit runs classes in parallel. Use `CommandScenario` for the valid path,
> test rejected state variants by instantiating the validator directly, and only
> for that case put the command's specifications in a small xUnit `[Collection]`.

## `EventScenario`

```csharp
readonly EventScenario _scenario = new();
IAppendResult _result;

async Task Establish() =>
    await _scenario.Given.ForEventSource(<IdentityType>.New())
        .Events(new <EventType>(<arguments>));

async Task Because() =>
    _result = await _scenario.EventLog.Append(
        <IdentityType>.New(), new <EventType>(<arguments>));

[Fact] void should_fail() => _result.ShouldBeFailed();

[Fact] void should_violate_<constraint>() =>
    _result.ShouldHaveConstraintViolationFor(<ConstraintNames>.<Name>);
```

`IAppendResult` assertions throw `AppendResultAssertionException`:
`ShouldBeSuccessful()`, `ShouldBeFailed()`, `ShouldHaveConstraintViolations()`
and `ShouldNotHaveConstraintViolations()`,
`ShouldHaveConstraintViolationFor(name)`,
`ShouldHaveConcurrencyViolations()` and its negation, `ShouldHaveErrors()` and
its negation. Assert the constraint **name**, never the message.

⚠️ `EventScenario` wires a no-op concurrency-scope strategy, so a concurrency
violation can never occur inside it. `ShouldHaveConcurrencyViolations()` cannot
pass there and its negation passes vacuously — specify concurrency against the
real kernel with an out-of-process integration specification instead.

## `ReadModelScenario<TReadModel>`

```csharp
ReadModelScenario<<ReadModelType>> _scenario = null!;

void Establish() => _scenario = new();

async Task Because() =>
    await _scenario.Given.ForEventSource(_id)
        .Events(new <EventType>(<arguments>), new <OtherEventType>());

[Fact] void should_<expected_state>() =>
    Assert.True(_scenario.Instance!.<Property>);
```

The helper auto-detects a model-bound projection, a fluent projection, or a
reducer. Use the named-parameter constructor when services or initial state are
needed: `new ReadModelScenario<<ReadModelType>>(initialState: null,
serviceProvider: services)`. Pre-seed a keyed read model with
`Given.ForEventSourceId(id).ReadModel(instance)` for code that calls
`IReadModels.GetInstanceById`. Cross-stream projections are supported — seed each
contributing stream with its own `Given.ForEventSource(…)`.

To bridge read models into a command scenario, build a focused
`ReadModelScenario<T>`, seed it, then register its read models into the command
scenario with `_scenario.Services.AddSingleton(readModelScenario.ReadModels)`.
For non-key searches, register a lookup double in the scenario services instead.

Do not pre-emptively skip a read-model assertion. Assume scalar concept, enum,
and identifier properties populate; if one does not, investigate the projection.
Skip only on a reproduced harness gap, and put the specific reason in the skip
message.

## `ReactorScenario<TReactor>`

```csharp
void Establish()
{
    _<collaborator> = Substitute.For<<CollaboratorType>>();
    _scenario = new(new ServiceCollection()
        .AddSingleton(_<collaborator>)
        .BuildServiceProvider());
}

async Task Because() =>
    await _scenario.Given.ForEventSource(_id).Events(new <EventType>(<arguments>));

[Fact] async Task should_<side_effect>() =>
    await _<collaborator>.Received(1).<Method>(<expected>);
```

Construct with an `IServiceProvider` of substituted collaborators and assert on
those substitutes after `Given` fires the events.

## Application conventions

- **Use a per-specification value for anything under a uniqueness check** — a
  fresh `Guid`, or a truncated `Guid.NewGuid().ToString("N")[..9]`. Hardcoded
  values cause order-dependent flakes. Do not add `[Collection(…)]` to work
  around a collision; that is the wrong fix here.
- **Sequence numbers are zero-based.** The first event is `0`, the tail of two
  events is `1`. The tail after a single append is `0`, never `1`.
- A behavior covering several subjects groups its specifications under
  `<Slice>/for_<Subject>/when_<behavior>/`.
