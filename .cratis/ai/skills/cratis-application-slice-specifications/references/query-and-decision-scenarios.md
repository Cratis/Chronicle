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

Start with a direct decision call supplied with read-model state. Follow the
[scenario rule](../../../rules/specs.scenarios.csharp.md): application authors do
not select read-protection modes, call `UseDecisionReads`, seed events only to
satisfy infrastructure, or prove the platform concurrency, conflict-refusal or
isolation matrix. Arc/Chronicle specs own it. The application's only decision-read
spec obligation is one cheap assembly-level check of its own model/projection
shapes, not one scenario per decision:

```csharp
DecisionReadAdmissions.ShouldAdmitDecisionReadsIn(assembly);
```

This API is in `Cratis.Arc.Chronicle.Testing.Commands`; it discovers declared reads
and checks admission without executing commands or running Chronicle. Keep the
spec's discovered projections representative of production; spec-only competing
definitions can make a model ambiguous. Explicit `IDecisionReads.Get` calls inside
method bodies are not statically discovered. If the harness cannot accept
required state for a business decision, report the upstream gap; never weaken
production guards or invent guard doubles.

Do not add a scenario to prove a race. A rule such as "two callers reserving the last
seat yield one reservation" is specified from state: supply a read model with no seats
left and assert the rejection, then supply one seat and assert the reservation.
Simulating competing appends is a framework concern a developer should not think
about in tests; Arc and Chronicle prove it. `AppendConcurrently` stays a public but
advanced framework-verification API (hidden from IntelliSense), outside application
testing guidance; use it only for a specific, explicit need. Production expresses
the guarantee with the read model as a `Handle` parameter under
`[ReadProtection(On)]`. Direction, not a release claim: Arc will obsolete
`UseDecisionReads`; do not adopt it.
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
- [Application-shape admission assertion](https://github.com/Cratis/Arc/blob/main/Source/DotNET/Chronicle.Testing/Commands/DecisionReadAdmissions.cs)
