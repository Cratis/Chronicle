<!-- cratis-ai-managed: skills/cratis-application-slice-specifications/references/query-and-decision-scenarios.md -->
# Query and decision scenarios

## Contents

- Query pipeline
- Decision specifications
- Sources

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

Use the plain `Specification` base for pure decisions;
these helpers add proof of framework composition, not HTTP transport.

## Query pipeline

`QueryScenario<TReadModel>` is in `Cratis.Arc.Testing.Queries` (`Cratis.Arc.Testing`).
Configure `Services` before the first call. `Perform(nameof(Model.Method),
arguments?, paging?, sorting?, cancellationToken?)` returns `QueryResult` through
conversion, validation, authorization and rendering. Use `QueryArguments` from
`Cratis.Arc.Queries`; there is no principal parameter. Register the principal
under test with `ICurrentPrincipalAccessor` in `Services`. Scheme-selected queries
fail closed in-process: HTTP authentication needs a hosted integration spec.

Both query and command scenarios preserve
explicit `ICurrentPrincipalAccessor`, `ICurrentPrincipalOverride` and
`IAuthorizationPolicyRuntime` registrations rather than replacing them with defaults.
Configure them before first execution and specify both authorized and denied callers.

Requires Arc 22.75.1 or later for query and command scenarios to preserve explicit principal and authorization services.
Source: `Source/DotNET/Testing/ScenarioServices.cs`, `Queries/QueryScenario.cs`,
`Commands/CommandScenario.cs`.

`Observe(...)` returns a disposable `QueryObservation`; `Initial` includes the
first result or rejection and `Next(timeout?, cancellationToken?)` reads the next
buffered emission (not necessarily distinct). Default deadlines are five seconds;
timeouts throw. Dispose asynchronously to await cleanup. Interceptors and guards
run when emissions are consumed. MongoDB `.Observe()` still needs a real DB with
change streams; the harness does not emulate it.

The discovered `IQueryScenarioExtender` supports additional context/services.
With `Cratis.Arc.Chronicle.Testing` and its Queries namespace, use
`Given.ForEventSource(id).ReadModel(instance)` or `.Events(...)` before performing.
The query must read through injected `IReadModels`; seeding does not replace
arbitrary stores.

Requires Arc 22.87.0 and Chronicle 19.54.0 for scenario `IReadModels.Observe<T>(filter)` snapshots from given events or pinned models,
including new emissions after scenario command appends. Pins need a readable key
or throw `PinnedReadModelCannotBeObserved`.
Requires Arc 22.91.1 and Chronicle 19.59.1 for children, joins and reducers to
observe through scenario-owned storage (the initial Arc package was not published).
Check both package versions; upgrade or report a harness gap rather than configuring
decision-read modes in application specs.

For a query with `by`, `filter` or `scoped to`, seed instances for two keys, two filter values
or two tenants with distinct callers, as applicable, and assert that each call returns only its
own instances, plus the empty case. This pins the caller-isolation decision in
`cratis-screenplay-read-surface` ("by vs filter"); caller scope must come from trusted context,
not a caller-supplied tenant filter. Compare whole returned items when the contract states every
value; otherwise assert each named value. A query backed by a database still needs that provider's
integration setup: scenario pins do not replace arbitrary stores.

`QueryScenario` owns and disposes its provider. Dispose the scenario (prefer
`DisposeAsync` for async-owned resources) as well as individual observations.
Register dependencies in `Services` before the first execution. Scenario
execution runs the query through Arc's hosted query pipeline and establishes context
before `Provide`/`Handle` on the command path. A direct static query call does not
cover those pipeline hooks.

## Decision specifications

Start with a direct `Handle()` or validator call supplied with read-model state;
validators own refusals. `Provide()` acquires data and only short-circuits
(`ValidationResult`/`AuthorizationResult`) when acquisition yields nothing usable,
not for ordinary validation rules. Follow the
[scenario rule](../../../rules/specs.scenarios.csharp.md): ordinary application
specs prove business decisions, not Arc/Chronicle's concurrency, conflict-refusal
or isolation matrix. Add pipeline scenarios only for the wiring they prove.
For that wiring, an admitted flat model pinned with `.ReadModel(instance)` can
serve a plain `Handle` parameter under `On`; pins are not advisory-only. Use routed
events for per-stream models and advanced `DecisionRead<T>` parameters, or when
mapping/history is the proof. `Given` needs `Cratis.Arc.Chronicle.Testing`.

### One assembly-level admission check

Keep one cheap admission spec over the application's command assemblies, not one
scenario per decision. This excerpt assumes `RegisterAuthor` is an application
command; include each command assembly in a multi-assembly application:

```csharp
using Cratis.Arc.Chronicle.Testing.Commands;

public class when_checking_decision_read_admission
{
    [Fact]
    public void should_admit_every_declared_protected_read() =>
        DecisionReadAdmissions.ShouldAdmitDecisionReadsIn(typeof(RegisterAuthor).Assembly);
}
```

The helper checks discovered declarations without executing commands or running
Chronicle. It inspects protected `Handle`/`Provide` decision-read parameters and,
under declared `On`, plain Chronicle-backed models and discoverable validator
constructor parameters, excluding advisory reads. Stream parameters use stream
admission; inherited methods are included and abstract commands are skipped in
assembly scans. It honors command/assembly declarations and ignores the runtime
cap. Keep discovered projections representative of production: a spec-local
projection for a production model can make admission ambiguous.

Explicit `IDecisionReads.Get<T>(key)` calls inside method bodies are not statically
visible. Give those models their own `Admit<T>()` check or a focused command
scenario. Do not treat the assembly check as coverage of those calls.

### Business decisions, not the platform race matrix

Do not add a scenario to prove a race already guaranteed by Arc/Chronicle. For
"two callers reserving the last seat yield one reservation", supply no-seat state
to the validator and assert rejection, then supply one-seat state and assert the
allowed decision. Direct calls do not prove the guard itself.

Typed `CommandScenario<TCommand>` selects decision reads from the command's
declarations. Do not call `UseDecisionReads()`; it is obsolete. For an untyped
`CommandScenario<object>` whose declarations cannot be selected at construction,
`EnableDecisionReads()` must run before seeding or the first `Execute`/`Validate`.
Do not change production protection for spec speed. If required state cannot be
supplied, report the harness gap rather than inventing guard doubles.

### Focused explicit-policy regression

An unusual application-owned explicit guard policy can justify a focused
regression that a plain call cannot prove. Examples are a revision captured during
a read and returned in `EventsWithConcurrencyScopes`, application-owned `[Unprotected]`
behavior on a command, model or parameter, or a deliberately non-atomic batch contract.
Test that chosen contract, not the platform's generic conflict/isolation matrix.
`EventsWithConcurrencyScopes` carries ordered events and supplied scopes into the
same command transaction, so the policy regression must exercise that boundary.

`AppendConcurrently` is an advanced framework-verification hook, hidden from normal
editor completion, not a routine application-spec template. Use it only for a
specific, explicit need: it works with or without decision mode and adds no guard
to an unguarded command. Queue it before the first execution; it appends competing
facts after response processing and before the owner's commit. Its route overload
preserves routing; nested commands do not drain the outer queue. It refuses custom
execution scopes whose ordering it cannot guarantee. Do not call `UseDecisionReads()`
to make the hook work, or hand-build a race matrix around it.
`ShouldHaveCompletedStream(ClosedStreamScope)` may assert that a decision completed a
stream at the exact source and route (`Cratis.Arc.Chronicle.Testing.Commands`).
Use event history only when it is part of the domain example.

The Chronicle-backed `CommandScenario` discovers constraints automatically.
Assert domain constraint refusals instead of disabling them; use `EventScenario`
for append-time uniqueness examples. Do not subclass or substitute its engine.
Provider boundaries remain separate: state fixtures are not a MongoDB substitute.

## Sources

- [Query scenarios](https://github.com/Cratis/Arc/blob/main/Documentation/backend/csharp/testing/query-scenario.md)
- [Stream decisions and routed specs](https://github.com/Cratis/Arc/blob/main/Documentation/backend/csharp/chronicle/commands/stream-decisions.md)
- [Decision-mode extension](https://github.com/Cratis/Arc/blob/main/Source/DotNET/Chronicle.Testing/Commands/CommandScenarioChronicleExtensions.cs)
- [Admission helper](https://github.com/Cratis/Arc/blob/main/Source/DotNET/Chronicle.Testing/Commands/DecisionReadAdmissions.cs)
- [Explicit revision scopes](https://github.com/Cratis/Arc/blob/main/Documentation/backend/csharp/chronicle/commands/concurrency.md)
- [Advisory read declarations](https://github.com/Cratis/Arc/blob/main/Source/DotNET/Chronicle/ReadModels/UnprotectedAttribute.cs)
- [Application-shape admission assertion](https://github.com/Cratis/Arc/blob/main/Source/DotNET/Chronicle.Testing/Commands/DecisionReadAdmissions.cs)
