---
applyTo: "**/for_*/**/*.cs, **/when_*/**/*.cs"
paths:
  - "**/for_*/**/*.cs"
  - "**/when_*/**/*.cs"
profile: application
---
<!-- cratis-ai-managed: rules/specs.scenarios.csharp.md -->

# Application Specs — the in-process scenario family

> **Application profile.** This file covers spec-writing for **event-sourced Cratis applications** — exercising commands, projections, reducers, reactors, and constraints with plain-call specs for pure decisions and the in-process scenario family where the pipeline matters. It builds on the universal [specs.csharp.md](./specs.csharp.md) (the `Specification` base + NSubstitute). **Framework / library** specs predominantly use that plain base instead; a framework repo reaches for a scenario helper here only when testing the very engine it provides — Arc tests its command pipeline with `CommandScenario`, Chronicle tests its event/projection/reactor engine with `EventScenario`/`ReadModelScenario`/`ReactorScenario`. That is the minority case, not the general framework testing mode.

Specs are **mandatory for every slice type**, including reactors.

## Start with the plain call; add a scenario where it adds proof

A command's `Handle()` is an ordinary method; with dependencies resolved by `Provide()`, its decision can be a pure function of its arguments. Reducer logic and a reactor's decision can be pure functions too. **Write the plain call as the default spec for a pure decision**, with no host, services or harness. Add a scenario helper where the framework's composition is part of what you must prove: the command pipeline, validation, authorization, constraints, and projections (declarative, so only a scenario can exercise them). The scenario helpers are additive; use both only when the slice needs both kinds of proof. For application behavior prefer the in-process helpers over full out-of-process Chronicle host specs.

| Tool | Tests | Use when |
|---|---|---|
| `Specification` (plain call) | `Handle()`, `Handle(providedValue)`, reducer logic or a reactor's decision as a method call; or an isolated class with mocked collaborators | **default** for a decision that is a pure function of its arguments |
| `CommandScenario<TCommand>` | the real Arc command pipeline — authorization + validators + `Provide()` + `Handle()` + appended events | the pipeline, validation, authorization, or appended events matter (State Change slices) |
| `QueryScenario<TReadModel>` | query pipeline: argument conversion, authorization, validation, interceptors; `Observe` buffers emissions | query composition matters; real MongoDB observations still need infrastructure |
| `EventScenario` | Chronicle-level append semantics, no command pipeline | constraint violations, raw stream sequencing/concurrency |
| `ReadModelScenario<TReadModel>` | projection/reducer state from a sequence of events (auto-detects model-bound / fluent / reducer) | **default for projections**, which are declarative (State View slices) |
| `ReactorScenario<TReactor>` | reactor handler invocation + side effects via mocked services | the reactor wiring and side effects (Automation / Translation slices) |

> **Out-of-process Chronicle integration specs are an advanced case** — reserve them for host wiring, real infrastructure, serialization/transport, or end-to-end boundaries the scenario helpers can't exercise. They are not the default vertical-slice test shape.

**Wrap every spec file in `#if DEBUG … #endif` when the specs live in the application assembly** beside the slice, as vertical slices place them, so spec code ships only in Debug (the Debug build gate validates it and also regenerates proxies). A dedicated spec project (`<Source>.Specs`) is never shipped and needs no wrapper; that is the case Arc's documentation describes.

### The plain call — the default for a pure decision

```csharp
#if DEBUG
namespace MyApp.Authors.Registration.when_registering_an_author;

public class and_all_information_is_valid : Specification
{
    readonly AuthorId _id = AuthorId.New();
    AuthorRegistered _event;

    void Because() => _event = new RegisterAuthor(_id, "Jane Austen").Handle();

    [Fact] void should_register_the_author() => _event.Name.ShouldEqual("Jane Austen");
}
#endif
```

Calling `Handle()` yourself runs no validation, authorization, `Provide()` or operations, so it proves the decision only. Add a `CommandScenario` spec for what the pipeline contributes.

### `CommandScenario<TCommand>` — the command pipeline

Runs authorization, validators, `Provide()` (when present), and `Handle()` in-process, and exposes the appended events.

```csharp
#if DEBUG
namespace MyApp.Authors.Registration.when_registering_an_author;

public class and_all_information_is_valid : Specification
{
    readonly CommandScenario<RegisterAuthor> _scenario = new();
    readonly AuthorId _id = AuthorId.New();
    CommandResult _result;

    async Task Because() => _result = await _scenario.Execute(new RegisterAuthor(_id, "Jane Austen"));

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] async Task should_have_appended_registered_event() =>
        await _scenario.ShouldHaveAppendedEvent<RegisterAuthor, AuthorRegistered>(_id, e => e.Name == "Jane Austen");
}
#endif
```

- `CommandScenario<TCommand>` exposes `Services` (an `IServiceCollection`), `Context`, `Execute(command[, ct])`, `Validate(command)`, `LastResult`, `Operations` and the operation assertions `ShouldHaveExecutedOperation<T>()` / `ShouldHaveCompensatedOperation<T>()` / `ShouldHaveNoOperationInvocations()`; with the Chronicle integration it also gains `Given`, `EventScenario`, `EventLog`, `EventSequence` and `AppendedEvents` as extension members. Everything else is configured through `Services` and asserted with extension methods.
- **Event assertions are extension methods on the scenario** (from the Chronicle testing package), keyed by **command + event** type: `await _scenario.ShouldHaveAppendedEvent<TCommand, TEvent>(eventSourceId)` or the `(eventSourceId, Func<TEvent,bool> predicate)` overload; plus `await _scenario.ShouldHaveTailSequenceNumber<TCommand>(...)`. They return `Task`, so the fact is `async Task`.
- **`CommandResult` assertions** (Arc extensions): `ShouldBeSuccessful()`, `ShouldNotBeSuccessful()`, `ShouldBeValid()`, `ShouldHaveValidationErrors()`, `ShouldHaveValidationErrorFor(message)`, `ShouldBeAuthorized()`, `ShouldNotBeAuthorized()`, `ShouldHaveExceptions()`/`ShouldNotHaveExceptions()`.
- **Seed prior state through `_scenario.Given` or `_scenario.Services`.** `_scenario.Given.ForEventSource(id).Events(...)` materializes the seeded events into an **in-memory read-model dictionary** the harness answers keyed reads from — in **legacy mode** it never reaches the event log (see the false-greens list below). To populate advisory read models the validator/`Provide()`/`Handle()` inject, substitute `IReadModels` and register it (`_scenario.Services.Replace(new ServiceDescriptor(typeof(IReadModels), mock))`, mocking `GetInstanceById(...)`) or register projections with `_scenario.Services.AddReadModels(...)`.
- **Validator/`Provide()` dependencies:** register them in `_scenario.Services`; Arc testing discovers the concrete validator automatically. When several specs need different injected validator states, test rejected variants by instantiating the validator directly (per-scenario state can be order-sensitive under parallel xUnit).
- **`Provide()`:** drive it through `CommandScenario` end-to-end; when the handler's decision is pure given provided data, test `Handle(providedValue)` directly as the default decision spec and keep the scenario for the wiring.
- **Command operations:** use **cratis-arc-command-operation** for decision/adapter specs and real `CommandScenario` execution, forward failure, compensation, and Chronicle commit rejection; a direct `Handle()` does not execute operations.

#### Validation-failure assertions — non-negotiable

Every unhappy-path spec asserts **both**:

```csharp
[Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();
[Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();
```

`ShouldNotBeSuccessful()` alone can't tell a validation rejection from an unhandled exception. **Do not assert on message strings by default** — they're presentation text; say which kind of rejection happened with `ShouldHaveValidationErrorBecauseOf(reason)`, or a constraint by name. Assert message text only when that exact text is the specified behavior, for example a rule whose wording the model pins, and say so in the fact's name. `ShouldHaveValidationErrorFor(message)` matches a substring, so it does not prove exact wording; compare for equality instead, for example `_result.ValidationResults.Select(_ => _.Message).ShouldContain("<exact text>")`. Authorization failures use `ShouldNotBeAuthorized()` (an unauthorized result has *no* validation errors, so `ShouldHaveValidationErrors()` would silently flip). To exercise a command that carries authorization attributes, register the identity the authorization evaluator reads into `_scenario.Services` (substitute the identity provider your app uses). **Adding `[Roles]` to an existing command breaks its happy-path AND validation-failure `.Execute()` specs** — an unauthorized result is not successful and carries no validation errors; switch those assertions to `ShouldNotBeAuthorized()`.

`CommandResult` assertions (from `Cratis.Arc.Testing.Commands`; failures throw `CommandResultAssertionException`): `ShouldBeSuccessful()` (`isAuthorized && isValid && !hasExceptions`), `ShouldNotBeSuccessful()`, `ShouldBeValid()` (no validation errors — does *not* check authz/exceptions), `ShouldHaveValidationErrors()`, `ShouldHaveValidationErrorFor(message)`, `ShouldHaveValidationErrorBecauseOf(ValidationResultReason)`, `ShouldHaveConstraintViolationFor(name)`, `ShouldBeAuthorized()`, `ShouldNotBeAuthorized()`, `ShouldHaveExceptions()`.

#### False greens the harness lets through

Each of these passes while proving nothing. They are the reason the two-assertion rule above exists.

1. **A discarded assertion `Task`.** The `IEventSequence` assertions `ShouldHaveAppendedEvent`/`ShouldHaveTailSequenceNumber` (Chronicle Testing) are genuinely `async`: in a `void [Fact]` that does not `await` them the exception lands on a `Task` nobody observes and the spec passes regardless of behavior, while the compiler's `CS4014` stays silent outside an `async` method. (The same-named `CommandScenario` overloads in Arc's Chronicle.Testing throw synchronously and only *return* a completed `Task`, so those fail even when discarded — but you cannot tell the two apart at the call site.) Make the fact `async Task` and `await` every `Task`-returning `Should*`. Chronicle's analyzer **`CHR0039` (`NonAwaitedAssertion`)** reports any discarded `Task`/`ValueTask`-returning `Should*` call from a Cratis testing package (Chronicle's and Arc's alike).
2. **A dependency the spec forgot to seed.** When a validator's constructor, `Provide()` or `Handle()` asks for a read model that does not exist for the command's key, the validator is never built and no rule runs — yet the command is rejected (one validation result, reason **`DependencyUnavailable`**, HTTP 400), so `ShouldNotBeSuccessful()` passes and keeps passing with the rule it is named after deleted. `ShouldHaveValidationErrors()` **refuses** a result whose only errors carry that reason and tells you to seed the read model — or, when that rejection *is* the behavior under spec, assert it deliberately with `ShouldHaveValidationErrorBecauseOf(ValidationResultReason.DependencyUnavailable)`. A missing *service* (not a read model) surfaces as an exception instead (`ShouldHaveExceptions()`), and `ShouldNotBeSuccessful()` alone cannot tell the two apart from a genuine rejection.
3. **`.Validate()` authorizes first.** `Validate(command)` runs the command filters, and authorization filters are ordered before validation. A validate-only spec on a `[Roles]`/`[Authorize]` command with no identity registered is denied for authorization — the rule it is named after never ran. Register the identity, or assert `ShouldNotBeAuthorized()` and say so in the name.
4. **Legacy-mode constraint preconditions go to the event log, not `CommandScenario.Given`.** `_scenario.Given.ForEventSource(id).Events(...)` on a `CommandScenario` seeds an **in-memory read-model materialization only**; nothing reaches the event log a `[Unique]`/`IConstraint` is enforced against, so a "second registration with the same name" spec passes because the first one was never appended. Seed constraint preconditions through **`EventScenario`**'s `Given`, and assert the violation by constraint **name** (`ShouldHaveConstraintViolationFor`) — the same helper exists on `CommandResult` and on `IAppendResult`.
5. **Legacy `GetInstances<T>` is not harness-backed.** Under legacy `CommandScenario` the read-model harness answers keyed reads (`GetInstanceById`) from its seeded dictionary and forwards `GetInstances<T>`/`GetSnapshotsById`/`Watch` to the real client, which is not there. Code under spec that enumerates a read model needs a fake lookup registered in `Services`, not `Given`.
6. **One child is not a children spec.** `[ChildrenFrom<T>]` without `key:` identifies children by the **event source id**, so every child event on one stream upserts the same child and a one-child spec stays green with the key wrong. Drive **two** children with distinct keys and assert the count.
7. **Independently derived expectations.** An expected value computed in the spec with the same expression as the code under spec agrees with it by construction. Write the expected literal.

The `ReadModelScenario` and `EventScenario` equivalents of these live in the **cratis-chronicle-read-model-specifications** and **cratis-chronicle-event-specifications** skills.

> **Historical validator-state order-sensitivity:** observed in the 22.16-era harness. Arc 22.23.2 stopped replacing the process-wide provider; later releases isolated scenario stores and event-source services. Reproduce at 22.75.0 before applying the old direct-validator/small `[Collection]` workaround. No current reproduction is verified here.

#### Query and protected-decision modes (Arc v22.75.0)

`QueryScenario<TReadModel>` (`Cratis.Arc.Testing.Queries`) runs `Perform(nameof(Model.Method),
arguments?, paging?, sorting?)`; `Observe(...)` returns `QueryObservation.Initial`
and buffered `Next()`. Configure principal/policies in `Services`; schemes fail
closed without HTTP. Chronicle query `Given` can seed keyed models/events.

For guarded commands use `UseDecisionReads()` **before** seeding events. Given
state, guarded reads and appends share one real in-process log, unlike legacy
mode above. Pinned models cannot supply guards. Use `OnRoute(route)` and
`AppendConcurrently` to prove conflicts; `ShouldHaveCompletedStream` proves
completion and `DecisionReadAdmissions.ShouldAdmitDecisionReadsIn(assembly)`
checks admission. See **cratis-application-slice-specifications**,
`references/query-and-decision-scenarios.md`, for sources and limitations.

### `EventScenario` — constraints & append semantics

```csharp
readonly EventScenario _scenario = new();
IAppendResult _result;

async Task Establish() =>
    await _scenario.Given.ForEventSource(AuthorId.New()).Events(new AuthorRegistered("Jane Austen"));

async Task Because() =>
    _result = await _scenario.EventLog.Append(AuthorId.New(), new AuthorRegistered("Jane Austen"));

[Fact] void should_be_failed() => _result.ShouldBeFailed();
[Fact] void should_violate_unique_constraint() => _result.ShouldHaveConstraintViolationFor(AuthorConstraintNames.UniqueName);
```

`IAppendResult` assertions (failures throw `AppendResultAssertionException`): `ShouldBeSuccessful()`, `ShouldBeFailed()`, `ShouldHaveConstraintViolations()`/`ShouldNotHave…`, `ShouldHaveConstraintViolationFor(name)`, `ShouldHaveConcurrencyViolations()`/`ShouldNotHave…`, `ShouldHaveErrors()`/`ShouldNotHave…`. Assert the constraint **name** by default; assert exact message text only when that wording is the specified behavior, and name that behavior in the fact.

### `ReadModelScenario<TReadModel>` — declarative projections and reducer wiring

The default for declarative projections; pure reducer decisions start with a direct call. Add this scenario for reducer dispatch, filtering, metadata or read-model wiring. It drives events into the projection/reducer and asserts the resulting state. Use xUnit `Assert.*` on `_scenario.Instance`.

```csharp
ReadModelScenario<Author> _scenario = null!;
void Establish() => _scenario = new();

async Task Because() =>
    await _scenario.Given.ForEventSource(_id).Events(new AuthorRegistered("Jane Austen"), new AuthorArchived());

[Fact] void should_be_archived() => Assert.True(_scenario.Instance!.IsArchived);
```

Use the named-parameter constructor when services/initial state are needed: `new ReadModelScenario<Author>(initialState: null, serviceProvider: services)`. Pre-seed a keyed read model with `Given.ForEventSourceId(id).ReadModel(instance)` for code that calls `IReadModels.GetInstanceById`. Cross-stream projections (`UsingKey`/`UsingParentKey`) are supported — seed each contributing stream with its own `Given.ForEventSource(...)`.

**Bridging `IReadModels` into a `CommandScenario`** (current harness limitation): when a command handler injects `IReadModels` directly, build a focused `ReadModelScenario<T>`, seed it via `Given.ForEventSourceId(id).ReadModel(...)`, then register its read models into the command scenario — `_scenario.Services.AddSingleton(readModelScenario.ReadModels)`. Pass `scenario.ReadModels` into code under test for keyed `GetInstanceById` reads; for non-key searches/filters, register a fake lookup or an `IMongoCollection<T>` mock in the scenario services instead.

**Chronicle v19.50.0 scenario additions** (`Source/Clients/Testing/ReadModels/`):
`WithProjection(Action<IProjectionBuilderFor<T>>)` overrides discovery, including
reducers. `Given.ForEventSource(id).OnStream(eventSourceType, eventStreamType,
eventStreamId).Events(...)` seeds routing for filtered reducers; unrouted events
do not reach a differently filtered handler. Dispose scenarios in `Destroy()`.
Per-run `Defaults(clientArtifactsProvider, serviceProvider, options)` supports
`InMemoryClientArtifactsProvider` and `ExplicitArtifacts` without global discovery.

**Skip discipline:** don't pre-emptively `[Fact(Skip=...)]` a read-model assertion — assume scalar `ConceptAs<string>`, enum, and identifier properties populate correctly; if one fails, investigate the projection. Only skip on a *reproduced* harness gap, and put the specific reason in the skip message.

### `ReactorScenario<TReactor>` — Automation / Translation

Construct with an `IServiceProvider` of NSubstitute mocks; assert on the mocks after `Given` fires events. Import `Cratis.Chronicle.Testing.Reactors;`.

```csharp
void Establish()
{
    _service = Substitute.For<IMyService>();
    _scenario = new(new ServiceCollection().AddSingleton(_service).BuildServiceProvider());
}
async Task Because() => await _scenario.Given.ForEventSource(_id).Events(new AuthorRegistered("Jane Austen"));
[Fact] async Task should_notify() => await _service.Received(1).Notify("Jane Austen");
```

In **Chronicle v19.50.0** (`Source/Clients/Testing/Reactors/`), `OnStream` also
applies to class-based and fluent reactors. `ProducedEvents` carries payload and
destination; `ShouldHaveProduced<TEvent>(eventSourceId)` or the predicate
`((produced, @event) => ...)` verifies cross-stream routing. The non-generic
`ReactorScenario(reactorId, defaults)` exercises an explicitly registered reactor.
Dispose each scenario in `Destroy()`; await async assertions (CHR0039).

#### Read-model setup — Chronicle 19.46.0+

In a class-based `ReactorScenario<TReactor>`, `Given` events invoke the reactor;
they **do not materialize read models**, even `[Passive]` ones. For each
read-model handler parameter, choose one setup before `Because()`:

```csharp
// Present-state specification: supply the instance the handler must receive.
void Establish() => _scenario.Given.ForEventSourceId(_id).ReadModel(_author);

// Separate absence specification: deliberately let an Author? parameter receive null.
void Establish() => _scenario.Given.ForEventSourceId(_id).NoReadModel<Author>();
```

These are alternatives in **separate specification classes**, not two
`Establish()` methods in one class. Both builders return
`ReactorSourceGivenBuilder<TReactor>` for chaining; neither setup is async.
Use the triggering event-source id by default, or the key returned by the
reactor's `ICanResolveReadModelKey.Resolve` when implemented. An absence
declaration is for that exact type and key and does not delete a seeded model.

If a recognized read-model parameter resolves to `null` without declared absence,
`Events(...)` throws `ReadModelNotSeededForReactorScenario` from
`Cratis.Chronicle.Testing.Reactors`, even if the parameter is nullable. The
message names the full type and missing key, explains `ReadModel(...)` and
`NoReadModel<T>()`, and lists seeded keys for that type when any exist. Fix the
fixture or its key; do not add `NoReadModel<T>()` just to silence missing setup.
The guard applies to `EventStoreForTesting`; it does not alter production
resolution or an explicitly supplied event store of another type.

**Handler failures:** in Chronicle 19.46.0+, a synchronous handler throw surfaces
its own exception, not `TargetInvocationException`, with its handler stack
preserved. Use `await Catch.Exception(() => _scenario.Given.ForEventSource(_id).Events(...))`
and assert the handler's exception type.

**Occurrence time:** in Chronicle 19.46.0+, `EventContext.From(...)` uses
`DateTimeOffset.UtcNow` when `occurred` is omitted (previously local `Now`).
`ReactorScenario` builds its contexts through this helper, as do
`EventContext.Empty` and `EmptyWithEventSourceId(...)`. Explicit occurrence times
keep their supplied offset; the positional constructor has no such default.

## Out-of-process Chronicle integration specs (advanced)

Reserve these for the host/transport boundary the scenario helpers can't reach. They test a complete slice — HTTP request → command → append → constraint → projection — against a real Chronicle store, and live under `when_<behavior>/` directly inside the slice folder (no `for_` folder; the "unit" is the whole slice).

```csharp
using context = MyApp.Authors.Registration.when_registering.and_name_already_exists.context;

namespace MyApp.Authors.Registration.when_registering;

[Collection(ChronicleCollection.Name)]
public class and_name_already_exists(context context) : Given<context>(context)
{
    public class context(ChronicleOutOfProcessFixture fixture) : given.an_http_client(fixture)
    {
        public const string AuthorName = "John Doe";
        public CommandResult<object>? Result;

        async Task Establish() => await EventStore.EventLog.Append(AuthorId.New(), new AuthorRegistered(AuthorName));
        async Task Because() => Result = await Client.ExecuteCommand<RegisterAuthor>("/api/authors/register", new RegisterAuthor(AuthorName));
    }

    [Fact] void should_not_be_successful() => Context.Result!.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_appended_only_one_event() => Context.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);
}
```

- `ChronicleCollection` is a project-defined `[CollectionDefinition]` class over `ChronicleOutOfProcessFixture` (Chronicle ships the fixture, not the collection).
- `ExecuteCommand<TCommand>(url, cmd)` returns `CommandResult<object>?`; `ExecuteCommand<TCommand, TResult>(url, cmd)` returns `CommandResult<TResult>?`.
- Helpers: `Context.ShouldHaveTailSequenceNumber(n)` (First = 0), `Context.ShouldHaveAppendedEvent<TEvent>(seq, eventSourceId, validator)`.

**Async reactor follow-ups** — when a reactor fires after the command and appends further events, collect them: start the collector **before** the triggering act with `_collector = StartCollectingAppends()`, then `await _collector.WaitForCount(2, TimeSpan.FromSeconds(10))`, assert via `_collector.ShouldHaveEvent<TEvent>(e => ...)`, and `_collector.Dispose()` in `Destroy()`. (`IEventAppendCollection` from the Chronicle testing API.)

## Application spec conventions

These supplement the universal conventions in [specs.csharp.md](./specs.csharp.md#conventions):

- **Per-test values for anything in a uniqueness check** — `$"{Guid.NewGuid():N}@example.com"`, a fresh `Guid`, or a truncated id `Guid.NewGuid().ToString("N")[..9]`. Hardcoded values cause order-dependent flakes. Do not add `[Collection(...)]` to work around *collisions* (it's the wrong fix here).
- **Sequence numbers are zero-based** — first event = `0`, tail of two events = `1`, tail of three = `2`. Never write "tail of 1" for a single appended event; the tail of `[A]` is `0`. (A common first-pass defect.)
- A slice covering **multiple subjects** (several read models/constraints/aspects) groups its specs under `<Slice>/for_<Subject>/when_<behavior>/`.

## See also

- [specs.csharp.md](./specs.csharp.md) — the universal `Specification` + NSubstitute base this builds on (and what framework specs use).
- [vertical-slices.md](./vertical-slices.md) — what each artifact promises (the contract under spec).
- [efcore.specs.md](./efcore.specs.md) — `DbContext` specs with SQLite in-memory.
- skills: **cratis-application-slice-specifications**, **cratis-chronicle-event-specifications**, **cratis-chronicle-read-model-specifications**.
