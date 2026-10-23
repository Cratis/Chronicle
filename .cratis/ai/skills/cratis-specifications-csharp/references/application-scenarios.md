<!-- cratis-ai-managed: skills/cratis-specifications-csharp/references/application-scenarios.md -->
# In-process scenario specifications

## Contents

- `CommandScenario<TCommand>`
- `EventScenario`
- `ReadModelScenario<TReadModel>`
- `ReactorScenario<TReactor>`
- Application conventions

The four scenario helpers exercise the real Arc and Chronicle pipelines
in-process. Use them for event-sourced *application* behavior where the pipeline,
validation, constraints or projections matter; a pure decision (`Handle()`,
`Handle(providedValue)`, reducer or reactor logic) is specified first as a plain
call. A library or framework repository reaches for a scenario only to test the
very engine it provides.

Read [query and decision scenarios](../../cratis-application-slice-specifications/references/query-and-decision-scenarios.md)
when using `QueryScenario<T>`, buffered emissions or guarded command scenarios.
Guarded decisions need actual seeded events, not pinned models; their given
state and appends share one in-process log, unlike legacy dictionary seeding.

| Helper | Exercises | Use for |
| --- | --- | --- |
| `CommandScenario<TCommand>` | Authorization, validators, `Provide()`, `Handle()`, appended events | State Change behavior |
| `QueryScenario<TReadModel>` | Argument conversion, authorization, validation, interceptors and buffered observations | Query pipeline behavior |
| `EventScenario` | Chronicle append semantics without the command pipeline | Constraints, raw sequencing, concurrency |
| `ReadModelScenario<TReadModel>` | Projection or reducer state from a sequence of events | State View behavior |
| `ReactorScenario<TReactor>` | Reactor invocation and its side effects | Automation and Translation behavior |

Wrap scenario specification files in `#if DEBUG … #endif` only when they compile
into the application assembly, so specification code ships only in Debug.
Dedicated spec projects need no wrapper. The example below uses the application
assembly convention.

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

`CommandScenario<TCommand>` itself exposes `Services`, `Context`, `Execute`
(two overloads), `Validate`, `LastResult`, `Operations`, the operation assertions
`ShouldHaveExecutedOperation<T>()` / `ShouldHaveCompensatedOperation<T>()` /
`ShouldHaveNoOperationInvocations()`, and `Dispose`/`DisposeAsync`. Seeding
(`Given`), the event log and the *event* assertions arrive as extension members
from `Cratis.Arc.Chronicle.Testing`.

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
- **Caller setup (Requires Arc 22.75.1):** register a substituted
  `ICurrentPrincipalAccessor`, `ICurrentPrincipalOverride` or
  `IAuthorizationPolicyRuntime` in `Services` before first execution. The scenario
  preserves these registrations instead of replacing them with Arc defaults,
  so role/policy success and denial specs run as the caller you supplied.
  Source: `Testing/ScenarioServices.cs` and `Testing/Commands/CommandScenario.cs`
  under `Source/DotNET/`.

### Pin generated values

Requires `Cratis.Arc.Testing` 22.67.0 or later.
For the `AllocateCartId` command from `cratis-arc-command`, whose
`Handle([GeneratedValue("cart")] CartId cartId)` returns that id, specify the
pipeline with a known concept pin. Import `Cratis.Arc.Commands` and
`Cratis.Arc.Testing.Commands`; the usual specification global usings supply
`Cratis.Specifications` and `Xunit`. In a dedicated spec project:

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Shopping.Carts.for_AllocateCartId.when_allocating;

public class and_value_is_pinned : Specification
{
    readonly CommandScenario<AllocateCartId> _scenario = new();
    readonly CartId _cartId = new(Guid.Parse("9a073b78-423c-43bc-b94c-19e4c83d1001"));
    CommandResult _result = default!;

    void Establish() => _scenario.PinGeneratedValue("cart", _cartId);
    async Task Because() => _result = await _scenario.Execute(new AllocateCartId());
    void Destroy() => _scenario.Dispose();

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] void should_return_the_pinned_id() =>
        ((CommandResult<CartId>)_result).Response.ShouldEqual(_cartId);
    [Fact] void should_capture_the_pinned_id() =>
        _scenario.GeneratedValues.Last<CartId>("cart").ShouldEqual(_cartId);
    [Fact] void should_capture_it_once() =>
        _scenario.GeneratedValues.All<CartId>("cart").ShouldContainOnly(_cartId);
}
```

- Pins use the attribute's explicit name, or the C# parameter name without one.
  Names are case-sensitive and pins are keyed by command type plus name.
- Supply the concept, not a raw `Guid`. Incompatible/null pins yield
  `PinnedGeneratedValueHasWrongType` during execution. Pins still undergo normal
  concept validation; only validated values are captured.
- Strict is the default. Without a pin, an unpinned generated parameter yields
  `GeneratedValueNotPinned` as an exception outcome in `CommandResult`; `Execute`
  does not throw it to the spec. Assert the unsuccessful/exception outcome when
  specifying that failure, not a request-validation rejection.
- `AllowGeneration()` opts into normal generation for unpinned values, including
  nested commands, when exact values do not matter. Explicit pins still win.
  Each pin supplies the same value on every execution; it is not a queue.
- For a child invoked through `ICommandPipeline`, configure
  `_scenario.PinGeneratedValue<ParentCommand, ChildCommand>("child", childId)`.
  The extension declares `PinGeneratedValue<TOtherCommand>` but C# requires
  **both** type arguments at the call site. This does not replace a same-named
  parent pin.
- `Last<T>(name)` returns the latest capture of that name and declared type,
  failing if absent. `All<T>(name)` returns a read-only snapshot in resolution
  order across executions (empty if none). Both include nested commands. Same
  name/type captures combine; use distinct names to distinguish commands.
- Configure pins and `AllowGeneration()` before the first execution, even if it
  fails or has no generated parameters. Changes after that throw
  `GeneratedValuesMustBePinnedBeforeExecution`; create a new scenario instead.
  Validation alone neither generates values nor freezes configuration.
- Do not log or trace captures. Do not replace/seed the production generator.
  A direct-call decision spec supplies the known argument to `Handle()` itself.

Guide: [Pin generated values in command specs](https://www.cratis.io/arc/backend/csharp/testing/generated-values/).
Source: `Source/DotNET/Testing/Commands/CommandScenarioGeneratedValueExtensions.cs`,
`ScenarioGeneratedValueFixture.cs`, `ScenarioGeneratedValues.cs` and
`GeneratedValuesCommandScenarioExtender.cs`;
`Source/DotNET/Testing.Specs/Commands/for_CommandScenario/generated_values/when_pinning_a_value.cs`.

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

Do not assert on message strings by default — they are presentation text.
Assert exact text only when that wording is the specified behavior, and name
that behavior in the fact.

See "Step 1" in SKILL.md for the order-sensitive validator caveat.

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
its negation. Assert the constraint **name** by default. Assert exact message
text only when that wording is the specified behavior, and name it in the fact.

See "Step 1" in SKILL.md for the vacuous concurrency assertions caveat.

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

### Read-model parameters

Requires Chronicle 19.46.0 or later.

`Given` events invoke the reactor; they **do not build read models**, including
passive ones. In the present-state specification, seed a pre-built instance:

```csharp
void Establish() => _scenario.Given.ForEventSourceId(_id).ReadModel(_author);
```

In a **separate absence specification**, deliberately let an `Author?` handler
parameter receive `null`:

```csharp
void Establish() => _scenario.Given.ForEventSourceId(_id).NoReadModel<Author>();
```

Both return `ReactorSourceGivenBuilder<TReactor>` for chaining, not a `Task`.
Use the triggering source id by default or `ICanResolveReadModelKey.Resolve`'s
key when implemented. Absence matches the exact type and key and does not remove
a seeded instance. Missing setup throws `ReadModelNotSeededForReactorScenario`
(`Cratis.Chronicle.Testing.Reactors`), **even for nullable parameters**. Its
message names the type/key, explains both setups, and lists other seeded keys
for that type when any exist. Fix the fixture/key rather than hiding missing
setup with `NoReadModel<T>()`. This guard applies to `EventStoreForTesting`, not
production resolution or an explicitly supplied store of another type.

A synchronous handler throw surfaces its own
exception, not reflection's `TargetInvocationException`, preserving the handler
stack. Capture with `await Catch.Exception(() => _scenario.Given.ForEventSource(_id).Events(...))`
and assert the handler's exception type.

`EventContext.From(...)` defaults omitted `occurred` to `DateTimeOffset.UtcNow`,
previously local `Now`. Scenario contexts, `EventContext.Empty` and
`EmptyWithEventSourceId(...)` use this helper; explicit times retain their offset,
and the positional constructor has no such default.

Sources:
`Source/Clients/Testing/Reactors/{ReactorScenario,ReactorSourceGivenBuilder,ScenarioReactorMethodArgumentsResolver,ReadModelNotSeededForReactorScenario}.cs`
and `Source/Clients/DotNET/Events/EventContext.cs`. For exact signatures and
source-backed examples, read `cratis-chronicle-reactor/references/specifications.md`.

## Application conventions

- **Use a per-specification fixture value for anything under a shared uniqueness
  check** — a fresh `Guid`, or a truncated `Guid.NewGuid().ToString("N")[..9]`.
  Hardcoded values in shared storage cause order-dependent flakes. This is
  fixture isolation, not advice to generate inside production `Handle()`;
  for generated parameters, pin the typed expected fixture value before
  execution. A known fixed pin is appropriate in an isolated scenario such as
  the allocation-only example above. Do not add `[Collection(…)]` to work around
  a collision; that is the wrong fix here.
- **Sequence numbers are zero-based.** The first event is `0`, the tail of two
  events is `1`. The tail after a single append is `0`, never `1`.
- A behavior covering several subjects groups its specifications under
  `<Slice>/for_<Subject>/when_<behavior>/`.
