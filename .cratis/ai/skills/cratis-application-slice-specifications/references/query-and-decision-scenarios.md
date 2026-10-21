<!-- cratis-ai-managed: skills/cratis-application-slice-specifications/references/query-and-decision-scenarios.md -->
# Query and decision scenarios

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
Requires Arc 22.91.1 and Chronicle 19.59.1 for default-mode children, joins and reducers to observe through scenario-owned storage (the initial Arc package was not published); older default mode cannot
observe children, so use decision mode. Check both package versions.

For a query with `by`, `filter` or `scoped to`, seed instances for two keys, two filter values
or two tenants with distinct callers, as applicable, and assert that each call returns only its
own instances, plus the empty case. This pins the caller-isolation decision in
`cratis-screenplay-read-surface` ("by vs filter"); caller scope must come from trusted context,
not a caller-supplied tenant filter. Compare whole returned items when the contract states every
value; otherwise assert each named value. A query backed by a database still needs that provider's
integration setup: scenario pins do not replace arbitrary stores.

## Guarded decisions

Call `new CommandScenario<TCommand>().UseDecisionReads()` before seeding. This
mode shares **one real in-process event log** among given events, guarded reads,
constraints and returned appends. Seed actual events, not pinned read models.
`Given.ForEventSource(id).OnRoute(route).Events(...)` preserves routing;
`scenario.EventRoutes.For<TSource>(stream).WithStreamId(...)` creates the route.
Queue interference using `AppendConcurrently(id, route, event)` and assert a
nonempty decision is rejected on the intended boundary. An empty identical-retry
append deliberately returns no scope and cannot prove the conflict guard.

Requires Arc 22.92.0 for `AppendConcurrently` without decision reads; older Arc
needs decision mode. Its early interference does not make a plain read guarded;
assert the actual scope/outcome, not merely that the competing append ran.

`ShouldHaveCompletedStream(type, id)` proves successful completion;
`ShouldHaveCompletedStream(ClosedStreamScope)` asserts the exact source and route.
`DecisionReadAdmissions.ShouldAdmitDecisionReadsIn(assembly)` checks projection
admission in assembly-wide specs. These APIs are in
`Cratis.Arc.Chronicle.Testing.Commands`. Routed seeding is refused in legacy mode.

The old warnings that command `Given` never reaches the event log and that its
read-model dictionary is the only backing are **legacy-mode** warnings, not a
reason to fake protected decision readers. Keep non-key enumeration/provider
boundaries explicit in either mode; decision mode is not a MongoDB substitute.

## Sources

- [Query scenarios](https://github.com/Cratis/Arc/blob/main/Documentation/backend/csharp/testing/query-scenario.md)
- [Stream decisions and routed specs](https://github.com/Cratis/Arc/blob/main/Documentation/backend/csharp/chronicle/commands/stream-decisions.md)
- [Decision-mode extension](https://github.com/Cratis/Arc/blob/main/Source/DotNET/Chronicle.Testing/Commands/CommandScenarioChronicleExtensions.cs)
- [Admission assertion](https://github.com/Cratis/Arc/blob/main/Source/DotNET/Chronicle.Testing/Commands/DecisionReadAdmissions.cs)
