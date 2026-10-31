---
name: cratis-application-slice-specifications
description: Specify the backend behavior of an event-sourced Cratis application slice — plain-call specs for pure decisions, plus CommandScenario, EventScenario, ReadModelScenario and ReactorScenario where the pipeline, validation, constraints or projections matter — including what to cover for each slice type and where the specification files live. Use when choosing backend plain-call coverage versus CommandScenario, EventScenario, ReadModelScenario or ReactorScenario for an event-sourced slice. Do not use for frontend interactions (use cratis-application-react-specifications) or framework unit-spec mechanics (use cratis-specifications-csharp).
license: MIT
metadata:
  cratis-hint-paths: "**/when_*/**/*.cs **/for_*/**/*.cs"
---
<!-- cratis-ai-managed: skills/cratis-application-slice-specifications/SKILL.md -->

# Cratis application slice specifications

Start a pure decision with a plain method call: `Handle()`,
`Handle(providedValue)`, reducer logic or a reactor's decision needs no host,
services or harness. Add a scenario where the pipeline, validation,
authorization, constraints or projections contribute proof. The scenario family
exercises the real Arc and Chronicle code paths in-process — no HTTP, no fixture,
no server.

This skill decides **when to add a scenario** and **what to cover**. The C# mechanics
(`Establish`/`Because`, substitutes, contexts) live in
`cratis-specifications-csharp`.

## Product packages

| Package | Purpose |
| --- | --- |
| `Cratis.Arc.Testing` | `CommandScenario<TCommand>` and the `CommandResult` assertions |
| `Cratis.Arc.Chronicle.Testing` | `Given`, `EventLog`, `AppendedEvents` and the appended-event assertions on a command scenario |
| `Cratis.Chronicle.Testing` | `EventScenario`, `ReadModelScenario<T>`, `ReactorScenario<T>` |
| `Cratis.Specifications.XUnit` | The `Specification` base and the `ShouldXxx` assertions |

Reactor read-model setup and the `EventContext.From` UTC default below need a newer Chronicle than
the Arc scenario surface itself; see `cratis-chronicle-reactor/references/specifications.md` for sources.

Requires Chronicle 19.46.0 or later for class-based reactor read-model seeding and the `EventContext.From` UTC default.

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
- The scope is model-first (the `specification` blocks of the `.play` slice are
  the contract for this skill). Decide this with the decision rule in
  `cratis-screenplay-modeling-lifecycle`: model-first only when an accepted
  model under the model root covers this scope, or the repository is opted in
  (opted in per `general.md` "Model first"; where `cratis-screenplay-modeling-lifecycle` is installed, run its `scripts/model-first-status.mjs`); framework, infrastructure, client, adapter and not-opted-in
  brownfield work stays code-first. Opted in but no model for this scope: model
  it first (`cratis-screenplay-discovery`, then `cratis-screenplay-slice-design`)
  before writing specifications. If those Screenplay skills are not installed,
  say so and stop; never author `.play` from memory. Never edit Stage-managed
  output, and never change a model specification to match existing code. Author or change model
  specifications with `cratis-screenplay-specifications` and
  `cratis-screenplay-scenario-coverage`; check finished code against the model
  with `cratis-application-slice-conformance`.

## Step 0 — Take the list from the contract

When the slice has a contract — a `.play` slice with its `specification`
blocks, or an agreed outline — its specifications are the **minimum list**: each
one becomes a specification class here, named after it. Steps 3–7 then add what
the code reveals (each validator rule, each constraint). A rule found in the code
with no contract specification is a proposal for the contract (the `.play` model when one covers the slice, otherwise the agreed outline), not silent coverage:
list it in the report and, where a model exists, route it to
`cratis-screenplay-scenario-coverage`.

Without a contract (code-first scope), derive the list from Step 3 and say that
you did.

### Reading a `.play` specification

Read the actual assignments, not a prose retelling; `given` seeds events or a pinned read model, `when` fills the command, `then` becomes the appended-event predicate or an instance assertion, and `then denied` is `ShouldNotBeAuthorized()`, not a validation error. Read [references/reading-play-specifications.md](references/reading-play-specifications.md) when translating a `.play` specification, including `parameter`/`case` tables, into a specification class.

## Step 1 — Place the files

Specifications live **in the slice folder**, beside the `.cs` they specify, each
file wrapped in `#if DEBUG … #endif` when it compiles into the application
assembly, so specification code ships only in Debug. A dedicated `<Source>.Specs`
project is not shipped and needs no wrapper.

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

## Step 2 — Start with the decision; add the scenario by slice type

Specify a pure decision with the plain `Specification` base first. A direct
`Handle()` call does not run validation, authorization, `Provide()` or operations;
keep a scenario for that wiring and for appended-event behavior.

| Slice type | Scenario | Skill |
| --- | --- | --- |
| Pure command decision, reducer logic or reactor decision | Plain method call with `Specification` (default) | `cratis-specifications-csharp` |
| State Change — command pipeline and appended events | `CommandScenario<TCommand>` | this one |
| Append semantics, a constraint | `EventScenario` | `cratis-chronicle-event-specifications` |
| Query pipeline — arguments, authorization, validation, interceptors | `QueryScenario<TReadModel>` | [query and decision scenarios](references/query-and-decision-scenarios.md) |
| State View — a projection or reducer | `ReadModelScenario<TReadModel>` | `cratis-chronicle-read-model-specifications` |
| Automation or Translation — a reactor | `ReactorScenario<TReactor>` | `cratis-specifications-csharp` |
| A host, transport, or real-infrastructure boundary | Out-of-process integration | `cratis-specifications-csharp` |

For a class-based reactor scenario, explicitly seed
handler read-model parameters with `Given.ForEventSourceId(id).ReadModel(instance)`
or declare intentional absence with `.NoReadModel<T>()`. An unseeded model throws
`ReadModelNotSeededForReactorScenario` even if nullable; `Given` events do not
build read models, including passive ones. Use the reactor's resolved key, not
necessarily the trigger's source id. This is **not** the command-scenario event
seeding described in Step 5. Read `cratis-chronicle-reactor` for the setup examples,
exception diagnostics, synchronous handler throws and UTC context defaults.

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
5. **Repeat execution**, when a reactor invokes the command: the same command
   twice for the same source. Specify whether the second is rejected, a no-op, or
   appends again — recovery can re-deliver an event, so the answer must be a
   decision, not an accident.

**Use the contract's example values.** When the contract gives concrete values
(the `when` and `then` values of a `.play` specification), use them, so the code
specification and the contract describe the same example. Where isolation needs
it, substitute a fresh identifier or uniqueness value (see *What breaks*) and keep
the example's relationships and behavior. Invent values only where the contract
is silent.

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

The scenario's surface (`Execute`, `Validate`, `LastResult`, `Operations`) is listed in [references/command-result-assertions.md](references/command-result-assertions.md); `Given` and the event assertions are extension members from the Chronicle integration.


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

That is **legacy** read-model mode: seeded events populate an in-memory model,
not the append log. A command under read protection `On`, with a per-stream model or
with `[ProtectedDecision]` selects the decision mode itself (no `UseDecisionReads()`
call); actual events and decisions then share one in-process log. Pinned models
cannot supply decision guards. Read
[query and decision scenarios](references/query-and-decision-scenarios.md) when
specifying query pipelines, routed conflicts, projection admission or completion.

Validator and `Provide()` dependencies are registered in `_scenario.Services`;
the concrete validator itself is discovered automatically.

## Step 6 — Assert twice on every unhappy path

```csharp
[Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();
[Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();
```

`ShouldNotBeSuccessful()` alone cannot distinguish a validation rejection from an
unhandled exception, so both facts are required. **Do not assert on a message
string by default** — it is presentation text; assert one only when that exact
text is the specified behavior, and name that requirement in the fact. A `.play`
specification may pin the message (`then error "<message>"`); prefer pinning the
*kind* in C# unless the exact wording is part of the behavior being specified.

**Isolate the rule.** Each rejection specification violates **only** the rule it
is named after: build the command valid in every other respect. Otherwise
`ShouldHaveValidationErrors()` passes when a neighbouring rule fires. Use
`ShouldHaveValidationErrorBecauseOf(reason)` where the reason distinguishes the
rule, and always run the "remove the rule, see it go red" check under *How it is
proven*.

Read [references/command-result-assertions.md](references/command-result-assertions.md) when choosing a `CommandResult` assertion: `ShouldHaveValidationErrorBecauseOf(reason)` is the message-free way to say which kind of rejection happened, and an unauthorized result carries no validation errors, so assert `ShouldNotBeAuthorized()`.

## Step 7 — Assert what was appended

The appended-event assertions come from `Cratis.Arc.Chronicle.Testing` and are
keyed by **command and event type**. They return `Task`, so the fact is
`async Task`, and they throw `EventSequenceAssertionException`.

- `ShouldHaveAppendedEvent<TCommand, TEvent>(eventSourceId)`
- `ShouldHaveAppendedEvent<TCommand, TEvent>(eventSourceId, Func<TEvent, bool> predicate)`
- `ShouldHaveTailSequenceNumber<TCommand>(sequenceNumber)`

⚠️ On a command scenario the second argument is a **predicate**
(`Func<TEvent, bool>`), not an assertion callback. The `Action<TEvent>` validator
shape lives on other receivers — the `IEventSequence` extensions in
`Cratis.Chronicle.Testing`, `AppendedEventWithResult.ShouldHaveEvent<TEvent>(Action<TEvent>?)`,
and the `IChronicleSetupFixture` extension in `Cratis.Chronicle.XUnit.Integration`
— never on `CommandScenario`.

Sequence numbers are **zero-based**: the first event is `0`, and the tail after a
single append is `0`, never `1`.

## Lineage

Step 0, repeat execution and rule isolation are adapted in our own words from
ideas recorded in `references/provenance.md`. Read `references/provenance.md` when checking attribution or the origin of Step 0, repeat-execution coverage, and rule isolation.

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
- **A rejection specification stays green when its rule is deleted.** The
  command was invalid in a second way, so another rule produced the error. Make
  the command valid except for the one rule, then repeat the red check.
- **The command scenario reports success but nothing was appended.** The
  assertion is on `CommandResult` only; add a
  `ShouldHaveAppendedEvent<TCommand, TEvent>` fact.
- **Injected-validator branches flip with test ordering.** An older harness replaced the process-wide provider; current
  releases isolate stores and event-source services per scenario. Reproduce
  before applying the old direct-validator/small `[Collection]`
  workaround.

## How it is proven

`dotnet build -c Debug` — it compiles the `#if DEBUG` specification code — then
`dotnet test`, both clean, with a specification for the happy path and one per
rejection. Before trusting a rejection specification green, remove the condition
that causes the rejection once and confirm it goes red.
