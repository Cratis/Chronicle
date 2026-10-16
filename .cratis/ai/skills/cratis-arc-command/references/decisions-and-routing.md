<!-- cratis-ai-managed: skills/cratis-arc-command/references/decisions-and-routing.md -->
# Protected decisions and routed streams

## Contents

- Pick the guarded read
- Definition-backed routes and templates
- Tags and completion
- Decision specifications
- Sources

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

Source: Arc. These features require `Cratis.Arc.Chronicle`.

## Pick the guarded read

Plain injected read models and `IReadModels.GetInstanceById` are advisory,
materialized or passive. A `Result<TEvent, ValidationResult>` rejects a request
but does not enroll a guard. Uniqueness and create-once belong to a Chronicle
constraint, not to any read. For the routing steps, the matrix and the traps read
[choosing how a rule holds](../../cratis-chronicle-decision-reads/references/choosing-how-a-rule-holds.md).

- For one stream, inject `IStreamReads` into `Provide`, call `ForCommand()`, and
  return `StreamDecision.Append(...)` from `Handle`. It returns
  `EventsWithConcurrencyScopes`, retaining the read route and exact tail. A
  competing append conflicts, including two competing first appends.
- For projected state, opt in with `[ProtectedDecision]` in
  `Cratis.Arc.Chronicle.ReadModels`, then inject `DecisionRead<T>` into
  `Provide`/`Handle` (`Exists`/`Instance`). Command-aware `IDecisionReads.Get<T>`
  enrolls reads of other keys. Only Chronicle-admitted direct event-source-keyed
  projections with a known event-type set qualify; reducers, joins, children,
  open-ended event types and incomplete definitions are refused. Arc certifies parameterless
  validators it constructs; constructor dependencies and uncertified instances
  are refused: a protected command's discoverable validator needs exactly one
  public parameterless constructor, otherwise the command fails with HTTP 500.
  The decision model needs an `Id` property, an event-source key and a flat
  projection; self-reducing models are refused like reducers. When the command has
  two or more key candidates, implement `ICanProvideEventSourceId`: `[Key]` does
  not silence `ARCCHR0002`, and Arc silently keys by the first eligible property
  in declaration order. Keep all decision state out of validators, including
  static members and service locators. `[Unprotected]` on the command opts out; never
  combine it with `[ProtectedDecision]`. A parameter-level marker acknowledges
  tooling, not a runtime opt-out. `Validate` is advisory, not a commit promise.
- `concurrency: true` on the routing attributes is **not** a guard for the
  decision: Arc reads the narrowed tail after `Handle`, so it only widens or
  narrows the window the default check covers. Exact returned scopes replace
  automatic capture; remove the flags when returning `EventsWithConcurrencyScopes`
  (ARCCHR0016). The default check also reads the tail after `Handle`, and the first
  append into a scope is unchecked unless `CheckFirstAppendIntoAScope` is set.

Requires Arc 22.81.0 for stream-scoped `IDecisionReads.Get<T>`,
`GetDetached<T>` and `AdmitForStream<T>` to participate in command protection,
caching and enrollment. Older Arc refuses these stream reads inside commands;
check the Arc package version before choosing this path. `[Unprotected]` command
reads are unguarded, not refused.

Do not manually append or commit to bypass the owner-capable unit of work.
The guard detects every matching event appended after its boundary, whoever wrote
it, but it protects only guarded decisions: an unguarded writer deciding on the
same facts can still append on stale state, so the rule holds only when every
deciding writer is guarded or the rule is a constraint. Protection does not guard an unguarded writer's own decision (a plain read,
`[Unprotected]` or a direct append), external state, revisions, redactions,
migrations, changes to projection definitions or writes that bypass the kernel's
serialized append path. Protection is per command and
opt-in; an unmarked command keeps its plain reads, and a conflict is returned as a
`ConcurrencyViolation` validation result with no automatic retry. A protected command that
stages no events commits an empty guarded batch: current kernels validate the
enrolled reads (a scope with an actual boundary or expecting no matching event) and write nothing, while a kernel that rejects an empty batch
raises `DecisionReadValidateOnlyNotSupported` (HTTP 500), so cover it with an
integration specification. In contrast, `StreamDecision.Append([])`
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
[EventStreamIdTemplate.cs](https://github.com/Cratis/Arc/blob/main/Source/DotNET/Chronicle/Commands/EventStreamIdTemplate.cs).

`IEventRoutes.For<TSource>(stream).WithStreamId(value)` resolves an `EventRoute`;
`IStreamReads.Stream(id, route)` guards that route, while `StreamType(id, route)`
guards every stream id of its type. Both require a specific source, stream type
and append stream id. There is only one exact scope per source id: do not combine
two independent stream decisions for the same source and assume both are guarded.
`route.Route(id, fact)` produces a routing wrapper. Explicit routing on a returned
`EventForEventSourceId` wins over command context in every return shape.

Requires Arc 22.79.0 for legacy wrapper-route validation on definition-routed
commands: a registered definition with an undeclared stream fails with
`EventStreamDoesNotBelongToEventSource`. ARCCHR0020 flags literal mistakes;
ARCCHR0021 recommends `EventSource = typeof(...)` and `EventStream` instead of
legacy strings. Older legacy routing can append to an undeclared stream; use the
typed definition route rather than relying on strings.

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
Requires Arc 22.81.0 and Chronicle kernel 19.39.0 for source-scoped completion:
a declared key, `ICanProvideEventSourceId` or response-supplied id scopes the
command's completion to that source (and resolved source type). A keyless command
still closes the stream type/id across the log; its internally generated id does
not scope completion. Older Arc closes across sources; an older kernel refuses
scoped completion, never falling back to all sources. Check both versions.
A failure leaves earlier events committed; retry completion rather than blindly
duplicating events. Reactors may return it directly, scoped to the observed
source id/type and stream route; reactor tuples/collections cannot mix it with events.

## Decision specifications

Call `new CommandScenario<T>().UseDecisionReads()` before seeding events.
`Given.ForEventSource(id).OnRoute(route).Events(...)` and
`AppendConcurrently(id, route, ...)` share one real in-process log. Pinned models
cannot supply guards. Assert a real nonempty decision conflicts on the intended
route, and that an independent route is not over-guarded.
`ShouldHaveCompletedStream(type, id)` asserts completion;
`ShouldHaveCompletedStream(ClosedStreamScope)` asserts the exact owner and route;
`DecisionReadAdmissions.ShouldAdmitDecisionReadsIn(assembly)` checks projection
admission in assembly-level specs.

## Sources

- [Stream decisions](https://github.com/Cratis/Arc/blob/main/Documentation/backend/csharp/chronicle/commands/stream-decisions.md)
- [Protected read-model decisions and limitations](https://github.com/Cratis/Arc/blob/main/Documentation/backend/csharp/chronicle/read-models/injecting-into-commands.md)
- [Concurrency and templates](https://github.com/Cratis/Arc/blob/main/Documentation/backend/csharp/chronicle/commands/concurrency.md)
- [Event tags](https://github.com/Cratis/Arc/blob/main/Documentation/backend/csharp/chronicle/commands/event-tags.md)
- [Completing streams](https://github.com/Cratis/Arc/blob/main/Documentation/backend/csharp/chronicle/commands/completing-streams.md)
