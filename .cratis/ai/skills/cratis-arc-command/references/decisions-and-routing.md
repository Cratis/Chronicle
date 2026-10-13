<!-- cratis-ai-managed: skills/cratis-arc-command/references/decisions-and-routing.md -->
# Protected decisions and routed streams

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

Verified at **Arc v22.75.0**. These features require `Cratis.Arc.Chronicle`.

## Pick the guarded read

Plain injected read models and `IReadModels.GetInstanceById` are advisory. A
`Result<TEvent, ValidationResult>` rejects a request but does not enroll a guard.

- For one stream, inject `IStreamReads` into `Provide`, call `ForCommand()`, and
  return `StreamDecision.Append(...)` from `Handle`. It returns
  `EventsWithConcurrencyScopes`, retaining the read route and exact tail. A
  competing append conflicts, including two competing first appends.
- For projected state, opt in with `[ProtectedDecision]` in
  `Cratis.Arc.Chronicle.ReadModels`, then inject `DecisionRead<T>` into
  `Provide`/`Handle` (`Exists`/`Instance`). Command-aware `IDecisionReads.Get<T>`
  enrolls reads of other keys. Only Chronicle-admitted direct event-source-keyed
  projections with a known event-type set qualify; reducers, joins, children,
  passive and incomplete definitions are refused. Arc certifies parameterless
  validators it constructs; constructor dependencies and uncertified instances
  are refused. Keep all decision state out of validators, including static
  members and service locators. `[Unprotected]` on the command opts out; never
  combine it with `[ProtectedDecision]`. A parameter-level marker acknowledges
  tooling, not a runtime opt-out. `Validate` is advisory, not a commit promise.
- Automatic `concurrency: true` on the routing attributes supplies stream-level
  scopes. Exact returned scopes replace automatic capture; remove the flags
  when returning `EventsWithConcurrencyScopes` (ARCCHR0016).

Do not manually append or commit to bypass the owner-capable unit of work.
Protection does not guard foreign writers, external state, revisions, redactions,
migrations or changes to projection definitions. A successful empty protected
command still validates enrolled reads. In contrast, `StreamDecision.Append([])`
returns no scopes: use it only for an identical retry, not a rejected request.

## Definition-backed routes and templates

`[EventSource<TSource>(stream)]` supplies source type and stream metadata on
commands and aggregate roots from a discovered definition. ARCCHR0013 recommends
it; ARCCHR0014 flags observer streams absent from that definition.

`[EventStreamId("{ReportingScopeId}:{Period}")]` resolves a command template after
validation. Concept properties contribute underlying values.
`EventStreamIdTemplate.ResolveFor(command)` exposes the same resolution.
ARCCHR0017/0018 check invalid templates/properties. On reactors the value is
literal, not a command template. `AmbiguousEventStreamId` rejects a non-NotSet
attribute value combined with `ICanProvideEventStreamId`, **including templates**.
Choose the attribute or the interface, not both. Source:
[EventStreamIdTemplate.cs](https://github.com/Cratis/Arc/blob/v22.75.0/Source/DotNET/Chronicle/Commands/EventStreamIdTemplate.cs).

`IEventRoutes.For<TSource>(stream).WithStreamId(value)` resolves an `EventRoute`;
`IStreamReads.Stream(id, route)` guards that route, while `StreamType(id, route)`
guards every stream id of its type. Both require a specific source, stream type
and append stream id. There is only one exact scope per source id: do not combine
two independent stream decisions for the same source and assume both are guarded.
`route.Route(id, fact)` produces a routing wrapper. Explicit routing on a returned
`EventForEventSourceId` wins over command context in every return shape.

## Tags and completion

In `Cratis.Arc.Chronicle.Commands`, `[EventTag("projectId", nameof(ProjectId))]`
takes a property and `[EventTag("origin", Value = "import")]` a constant. Each
attribute must have exactly one source. `ICanProvideEventTags` computes command
tags; returned `EventTags` supplies handler-known tags; discovered scoped/transient
`ICanProvideCommandEventTags` providers add application conventions. All sources
form a union distinct by name/value pair; wrapper tags are preserved. Tags apply
to returned events, not manual appends or aggregate commits.

Return `CompleteStream` (`Cratis.Arc.Chronicle.Streams`) alone or with events in
a command tuple. It completes **after commit**, not atomically with append.
Completion is keyed by stream type/id across the entire event log, not source id.
A failure leaves earlier events committed; retry completion rather than blindly
duplicating events. Reactors may return it directly, using the observed route;
reactor tuples/collections cannot mix it with events.

## Decision specifications

Call `new CommandScenario<T>().UseDecisionReads()` before seeding events.
`Given.ForEventSource(id).OnRoute(route).Events(...)` and
`AppendConcurrently(id, route, ...)` share one real in-process log. Pinned models
cannot supply guards. Assert a real nonempty decision conflicts on the intended
route, and that an independent route is not over-guarded.
`ShouldHaveCompletedStream(type, id)` asserts completion;
`DecisionReadAdmissions.ShouldAdmitDecisionReadsIn(assembly)` checks projection
admission in assembly-level specs.

## Sources at v22.75.0

- [Stream decisions](https://github.com/Cratis/Arc/blob/v22.75.0/Documentation/backend/csharp/chronicle/commands/stream-decisions.md)
- [Protected read-model decisions and limitations](https://github.com/Cratis/Arc/blob/v22.75.0/Documentation/backend/csharp/chronicle/read-models/injecting-into-commands.md)
- [Concurrency and templates](https://github.com/Cratis/Arc/blob/v22.75.0/Documentation/backend/csharp/chronicle/commands/concurrency.md)
- [Event tags](https://github.com/Cratis/Arc/blob/v22.75.0/Documentation/backend/csharp/chronicle/commands/event-tags.md)
- [Completing streams](https://github.com/Cratis/Arc/blob/v22.75.0/Documentation/backend/csharp/chronicle/commands/completing-streams.md)
