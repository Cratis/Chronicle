---
applyTo: "**/for_*/**/*.cs, **/when_*/**/*.cs"
paths:
  - "**/for_*/**/*.cs"
  - "**/when_*/**/*.cs"
profile: application
---
<!-- cratis-ai-managed: harnesses/copilot/instructions/specs.scenarios.csharp.instructions.md -->
<!-- cratis-ai: generated Copilot instructions. Do not edit; rules/ is the source. -->

# Application Specs — the in-process scenario family

> **Application profile.** This file covers spec-writing for **event-sourced Cratis applications** — exercising commands, projections, reducers, reactors, and constraints with plain-call specs for pure decisions and the in-process scenario family where the pipeline matters. It builds on the universal [specs.csharp.md](./specs.csharp.instructions.md) (the `Specification` base + NSubstitute). **Framework / library** specs predominantly use that plain base instead; a framework repo reaches for a scenario helper here only when testing the very engine it provides — Arc tests its command pipeline with `CommandScenario`, Chronicle tests its event/projection/reactor engine with `EventScenario`/`ReadModelScenario`/`ReactorScenario`. That is the minority case, not the general framework testing mode.

Specs are **mandatory for every slice type**, including reactors.

## Start with the plain call; add a scenario where it adds proof

Call pure `Handle()`, validators, reducers and reactor decisions directly with their inputs, including read-model state. Add a scenario only for the wiring the decision table below obligates (pipeline and authorization, constraints, projection and reducer dispatch, reactor dispatch and routing). Do not repeat every decision through the pipeline; name the extra behavior each scenario proves.

**Decision table — apply it per artifact, without judgment.** A plain call proves the decision; a scenario is a coverage obligation for the wiring only the framework proves. These are obligations, not quotas: add a scenario for each behavior listed, and none beyond it.

| Artifact | Plain call (`Specification`) | Scenario obligation |
|---|---|---|
| Command with a pure `Handle(…)` | every outcome and rejection branch, with read-model state supplied | the pipeline path once (`CommandScenario`: discovery, DI, event source, appended events), plus a **negative proof for each wiring that can refuse before `Handle`**: authorization denies (`ShouldNotBeAuthorized`), validation rejects, `Provide()` short-circuits |
| `Handle` that needs `IEventStore`, `IReadModels` or services | none: do not substitute the store | keep the scenario; when touched, take the read model as a `Handle` parameter and use the row above |
| `CommandValidator`, `ConceptValidator`, `Provide()` logic | each rule, built with its state | the command's pipeline scenarios prove the wiring |
| Constraint (`[Unique]`, `IConstraint`) | none | an `EventScenario` per constraint: the violation and the nearest non-violation |
| Projection or read model | none | `ReadModelScenario` against the real engine, covering **each event, key, join, children and removal the projection declares**: one class per read model with one shared event sequence by default; add a class only for a sequence that cannot share it (a removal, another key) |
| Reducer | each transition: `Model.Reduce(event, current[, context])` | a `ReadModelScenario` for dispatch, filters and stream routing |
| Reactor | each handler decision and side effect, called directly with substitute collaborators | a `ReactorScenario`: dispatch, filtering, read-model parameters, routing of produced events to the right event source, `[OnceOnly]`/replay |
| Query | extracted filter, sort or composition logic | `QueryScenario` for arguments, authorization and paging; provider behavior is integration |
| Cross-slice flow | each link | out-of-process integration, outside the fast loop |

Name a scenario after the wiring it proves (`and_the_caller_is_not_a_maintainer`). A plain `Handle()` spec never takes a validator, authorization or constraint name: it did not run them. Merge scenario classes only with an assertion map that keeps every assertion. Never use a substitute for the event store or a read-model store as proof of persistence or projection. Cost and parallel-safety requirements are in [fast-feedback.md](./fast-feedback.instructions.md).

#### Negative authorization proof

```csharp
#if DEBUG
namespace MyApp.Authors.Registration.when_registering_an_author;

public class and_the_caller_is_not_a_maintainer : Specification
{
    readonly CommandScenario<RegisterAuthor> _scenario = new();   // no identity registered
    CommandResult _result;

    async Task Because() => _result = await _scenario.Execute(new RegisterAuthor(AuthorId.New(), "Jane Austen"));

    [Fact] void should_be_denied() => _result.ShouldNotBeAuthorized();
    [Fact] async Task should_append_nothing() => await _scenario.ShouldNotHaveAppendedEvents();
}
#endif
```

An unauthenticated scenario against an `[Authorize]`/`[Roles]` command is denied before `Handle`; pair it with the happy-path scenario that registers an allowed identity in `_scenario.Services`.

**Application specs supply state, not decision-read infrastructure.** Call `Handle()` or the validator directly with supplied read-model state. Never call `UseDecisionReads`, configure protection modes, seed events solely for infrastructure, or re-prove the platform's concurrency, conflict-refusal or isolation matrix; Arc/Chronicle specs own it. The application's **only decision-read spec obligation** is one cheap assembly-level shape-admission spec: `DecisionReadAdmissions.ShouldAdmitDecisionReadsIn(assembly)` (`Cratis.Arc.Chronicle.Testing.Commands`). It checks the application's own models/projections without executing commands or running Chronicle, not the platform guards. Never simulate competing appends either (`AppendConcurrently` or any hand-built interleaving): concurrency is a framework concern a developer should not think about in tests, and Arc and Chronicle specs prove it. A rule that must hold under races is expressed in production by taking the read model as a `Handle` parameter under `[ReadProtection(On)]`; the spec gives that read model's state and asserts the outcome. Report harness state gaps upstream; never weaken production guards for speed. `AppendConcurrently` is an advanced framework-verification API: use only for an explicit need. Direction, no release claim: Arc will obsolete `UseDecisionReads`.

**Share setup and budget cost (judgment-shaped).** Keep shared lifecycle on; use `[PerFactSpecificationLifecycle]` only on a class whose facts genuinely mutate shared state and cannot move that mutation into `Because`. Plain decision specs should take roughly milliseconds; measure scenario facts and justify seconds by the extra behavior they prove. Compare TRX durations and suite wall time with unchanged concurrency; see [running-specs.md](../../../skills/cratis-specifications-csharp/references/running-specs.md). Do not multiply scenario startup by splitting assertions into classes or opting the project out of shared lifecycle.

| Tool | Tests | Use when |
|---|---|---|
| `Specification` (plain call) | `Handle()`, `Handle(providedValue)`, validator/reducer logic or a reactor's decision as a method call; or an isolated class with mocked collaborators | **default** for a decision that is a pure function of its arguments |
| `CommandScenario<TCommand>` | the real Arc command pipeline — authorization + validators + `Provide()` + `Handle()` + appended events | pipeline wiring, authorization or constraints adds proof |
| `QueryScenario<TReadModel>` | query pipeline: argument conversion, authorization, validation, interceptors; `Observe` buffers emissions | query composition matters; real MongoDB observations still need infrastructure |
| `EventScenario` | Chronicle-level append semantics, no command pipeline | constraint violations, raw stream sequencing/concurrency |
| `ReadModelScenario<TReadModel>` | projection/reducer state from a sequence of events (auto-detects model-bound / fluent / reducer) | one class per read model by default, many read-only assertions on shared setup (see the decision table for when to add another) |
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
- **Supply read-model state for business decisions.** Prefer a direct call with that state; for pipeline proof, use the harness's state setup. Seed events only when the event history itself is relevant, not to accommodate read-protection plumbing.
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
4. **Constraint preconditions must exist at the append boundary.** Use **`EventScenario`**'s `Given` for a duplicate-name constraint, and assert the violation by name (`ShouldHaveConstraintViolationFor`). A pinned read model alone cannot prove append-time uniqueness.
5. **Advisory `IReadModels` is not a database provider.** `CommandScenario` backs keyed `GetInstanceById(ReadModelKey)` reads, but forwards `GetInstances<T>`, `GetSnapshotsById` and `Watch` to the real client. For a focused pipeline spec whose handler injects advisory `IReadModels`, bridge keyed state from a `ReadModelScenario<T>` seeded with `Given.ForEventSourceId(id).ReadModel(instance)`, registering `_scenario.Services.AddSingleton(readModelScenario.ReadModels)` before execution. For non-key searches/filters, register a lookup double in `Services`; event seeding alone does not back them. This is advisory state setup, not decision-read protection plumbing; pure decisions still take state directly.
6. **One child is not a children spec.** `[ChildrenFrom<T>]` without `key:` identifies children by the **event source id**, so every child event on one stream upserts the same child and a one-child spec stays green with the key wrong. Drive **two** children with distinct keys and assert the count.
7. **Independently derived expectations.** An expected value computed in the spec with the same expression as the code under spec agrees with it by construction. Write the expected literal.

The `ReadModelScenario` and `EventScenario` equivalents of these live in the **cratis-chronicle-read-model-specifications** and **cratis-chronicle-event-specifications** skills.

> **Historical validator-state order-sensitivity:** observed in the 22.16-era harness. Arc 22.23.2 stopped replacing the process-wide provider; later releases isolated scenario stores and event-source services. Reproduce at 22.75.0 before applying the old direct-validator/small `[Collection]` workaround. No current reproduction is verified here.

#### Query pipeline

`QueryScenario<TReadModel>` (`Cratis.Arc.Testing.Queries`) runs `Perform(nameof(Model.Method),
arguments?, paging?, sorting?)`; `Observe(...)` returns `QueryObservation.Initial`
and buffered `Next()`. Configure principal/policies in `Services`; schemes fail
closed without HTTP. Chronicle query `Given` can seed keyed models/events.

For query setup, see **cratis-application-slice-specifications**,
`references/query-and-decision-scenarios.md`. Framework read-protection guards are not application spec obligations.

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

The default for declarative projections; pure reducer decisions start with a direct call. Use **one scenario class per read model**, not one per property, mapping attribute or lifecycle event; add another class only for an event sequence that cannot share the first (a removal, another key), as the decision table says. Apply its event sequence once, capture intermediate states in `Because` where needed, then assert fields, children, counters and lifecycle outcomes in many read-only facts. Add a reducer scenario only for dispatch, filtering, metadata or read-model wiring. Use xUnit `Assert.*` on `_scenario.Instance`.

```csharp
ReadModelScenario<Author> _scenario = null!;
void Establish() => _scenario = new();

async Task Because() =>
    await _scenario.Given.ForEventSource(_id).Events(new AuthorRegistered("Jane Austen"), new AuthorArchived());

[Fact] void should_be_archived() => Assert.True(_scenario.Instance!.IsArchived);
```

Use the named-parameter constructor when services/initial state are needed: `new ReadModelScenario<Author>(initialState: null, serviceProvider: services)`. Pre-seed a keyed read model with `Given.ForEventSourceId(id).ReadModel(instance)` for code that calls `IReadModels.GetInstanceById`. Cross-stream projections (`UsingKey`/`UsingParentKey`) are supported — seed each contributing stream with its own `Given.ForEventSource(...)`.

Do not build a projection scenario just to supply state to a pure command decision. Pass the read model directly, or substitute the application's lookup collaborator. Provider behavior belongs in provider integration specs.

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

These supplement the universal conventions in [specs.csharp.md](./specs.csharp.instructions.md#conventions):

- **Per-test values for anything in a uniqueness check** — `$"{Guid.NewGuid():N}@example.com"`, a fresh `Guid`, or a truncated id `Guid.NewGuid().ToString("N")[..9]`. Hardcoded values cause order-dependent flakes. Do not add `[Collection(...)]` to work around *collisions* (it's the wrong fix here).
- **Sequence numbers are zero-based** — first event = `0`, tail of two events = `1`, tail of three = `2`. Never write "tail of 1" for a single appended event; the tail of `[A]` is `0`. (A common first-pass defect.)
- A slice covering **multiple subjects** (several read models/constraints/aspects) groups its specs under `<Slice>/for_<Subject>/when_<behavior>/`.

## See also

- [specs.csharp.md](./specs.csharp.instructions.md) — the universal `Specification` + NSubstitute base this builds on (and what framework specs use).
- [vertical-slices.md](./vertical-slices.instructions.md) — what each artifact promises (the contract under spec).
- [efcore.specs.md](./efcore.specs.instructions.md) — `DbContext` specs with SQLite in-memory.
- skills: **cratis-application-slice-specifications**, **cratis-chronicle-event-specifications**, **cratis-chronicle-read-model-specifications**.
