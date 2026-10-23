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

Under read protection `On` (`<CratisReadProtection>On</CratisReadProtection>`, or
`[ReadProtection(ReadProtectionMode.On)]` on a command), a read model that is a
parameter of `Handle()` or of a `CommandValidator<T>` constructor is read fresh from
the event log and guards the command's append. `Off` is the default when nothing is
set; under `Off` and `Measure` those reads are plain, a hint that guards nothing.
A `Result<TEvent, ValidationResult>` rejects a request but does not enroll a guard.
Uniqueness and create-once belong to a Chronicle constraint, not to any read. For the
routing by rule shape, the matrix and the traps read
[choosing how a rule holds](../../cratis-chronicle-decision-reads/references/choosing-how-a-rule-holds.md).

Requires Arc 22.106.0 for read protection. Older Arc has only the per-command
`[ProtectedDecision]` path below.

- **One entity's state.** Put the rule in a validator with the read model as a
  constructor parameter; `Handle()` returns the events. The key is
  `[LookUpBy(nameof(Cmd.Prop))]` on the parameter (`Cratis.Arc.Commands`), else the
  single command property whose type equals the model's `Id` type, else the
  command's event source id. Several matching properties fail with
  `AmbiguousReadModelLookup` (500): add `[LookUpBy]`.
- **Two entities.** Two read-model parameters, the second with `[LookUpBy]`;
  `Handle()` returns `IEnumerable<EventForEventSourceId>`, one event per entity,
  appended atomically. Use a distinct event type per entity.
- **One stream inside a source.** A per-stream read model
  (`[ReadModel, EventStream<TSource>("Transactions")]`, `Cratis.Chronicle.ReadModels`)
  as a plain parameter, on a command routed `[EventSource<TSource>("Transactions")]`
  and `[EventStreamId("{Month}")]`; otherwise `ReadModelStreamNotOnCommandRoute`
  (500, analyzer ARCCHR0037). It is folded from that one stream and key-only reads
  are refused. For raw events of a short stream, inject `IStreamReads` into
  `Provide`, call `ForCommand()`, and return `StreamDecision.Append(...)` from
  `Handle` (`EventsWithConcurrencyScopes`); a competing append conflicts, including
  two competing first appends.
- **Computed key or another stream or source.** Command-aware `IDecisionReads.Get<T>`
  in `Provide()`, including the stream overloads.
- **Rich lifecycle.** An aggregate root, with `Commit()` returned last. Under `On`,
  an aggregate and a protected read of the same source are refused
  (`ReadAndAggregateOnSameSource`); a different source is fine.
- **Editing conflict.** Return `EventsWithConcurrencyScopes` built from the revision
  the client sent. `concurrency: true` on the routing attributes is **not** a guard
  for the decision: Arc reads the narrowed tail after `Handle`. Remove the flags
  when returning exact scopes (ARCCHR0016). The default check also reads the tail
  after `Handle`, and the first append into a scope is unchecked unless
  `CheckFirstAppendIntoAScope` is set.
- **No guard needed.** A hint or a rare violation: mark a deliberate advisory read
  `[Unprotected("why")]` (`Cratis.Arc.Chronicle.ReadModels`; on a method it covers
  the body, not injected parameters), or repair with a reactor.

Requires Arc 22.113.0 with Chronicle 19.76.0 for `ReadChanged` validation state
to include the conflicting event type/id and sequence number when the kernel
reports them. Older kernels can leave those details absent. Outside-decision
read-model injection (for example a custom command filter) is refused under
`On`; explicitly unprotected models stay advisory. Check `dotnet list package`
before asserting conflict detail fields.

Requires Arc 22.116.2 for aggregate application, rehydration and append to resolve
events from registrations, including explicitly registered types without an
`[EventType]` attribute. Unregistered events still fail; do not omit registration.

Under `On`, `IReadModels`, `IMongoCollection`, detached reads and immediate appends
through an injected `IEventLog` are refused; take the model as a parameter and
return the events. Admitted models: event-source-keyed model-bound or declarative
projections with a string or Guid `Id`, and unfiltered reducers without constructor
dependencies except `ILogger<T>` (self-reducing models are exempt). Joins and
children, event-property keys, Pinned delivery, filtered reducers, reducers with
dependencies and other event sequences fail with `ReadCannotBeProtected` (500)
unless marked `[Unprotected("why")]`. Validators with read models must be
convention-registered (transient).

When the command has two or more key candidates, implement `ICanProvideEventSourceId`:
`[Key]` does not silence `ARCCHR0002`, and Arc silently keys by the first eligible
property in declaration order.

A lost race is HTTP 400 with a validation result of reason `concurrencyViolation`,
`reasonDetail` `ReadChanged` and `state` `{ readModel, key }`. Arc and Chronicle do
not re-run the command; the client submits it again. Every other refusal is 500:
change the code.

### Per-command opt-in: `[ProtectedDecision]`

`[ProtectedDecision]` (`Cratis.Arc.Chronicle.ReadModels`) with `DecisionRead<T>`
(`Exists`/`Instance`) is the older per-command opt-in. It still works and is not
obsolete; do not describe `On` as making it redundant. Use it for a command in a
project that is not on `On`. Without `On`, only a parameterless validator runs: a
discoverable validator needs exactly one public parameterless constructor,
otherwise the command fails with HTTP 500. Keep decision state out of validators
there, including static members and service locators. `[Unprotected]` on the
command opts out; never combine it with `[ProtectedDecision]`
(`CommandCannotBeBothProtectedAndUnprotected`).

Requires Arc 22.81.0 for stream-scoped `IDecisionReads.Get<T>`,
`GetDetached<T>` and `AdmitForStream<T>` to participate in command protection,
caching and enrollment. Older Arc refuses these stream reads inside commands.

Do not manually append or commit to bypass the owner-capable unit of work.
The guard detects every matching event appended after its boundary, whoever wrote
it, but it protects only guarded decisions: an unguarded writer deciding on the
same facts can still append on stale state, so the rule holds only when every
deciding writer is guarded or the rule is a constraint. Outside the guard:
`[Unprotected]` reads, `Off` mode, external state, revisions and redactions,
migrations, changes to projection definitions and writes that bypass the kernel's
serialized append path. A protected command that stages no events commits an empty
guarded batch: current kernels validate the enrolled reads and write nothing, while
a kernel that rejects an empty batch raises `DecisionReadValidateOnlyNotSupported`
(HTTP 500), so cover it with an integration specification. In contrast,
`StreamDecision.Append([])` returns no scopes: use it only for an identical retry,
not a rejected request.

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

`new CommandScenario<T>()` selects the in-process decision log from the command's
read-protection mode and dependencies (under `On`, with a per-stream model or
`[ProtectedDecision]`); `UseDecisionReads()` is hidden and unnecessary. An `Off`
command with no protected dependency stays in the legacy mode with pinned read
models, which cannot prove a race. Seed with `Given.ForEventSource(id).Events(...)`,
`.OnRoute(route).Events(...)`, queue a competing append with
`AppendConcurrently(id, events...)` (or `AppendConcurrently(id, route, ...)`) and
assert `result.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.ConcurrencyViolation)`.
There is no `scenario.AppendConditions`. Guarded state needs seeded events: protected reads ignore a pinned `.ReadModel(m)` (which only pins an advisory plain read) and `DecisionRead<T>` rejects it. Assert a
real nonempty decision conflicts on the intended route, and that an independent
route is not over-guarded. `ShouldHaveCompletedStream(type, id)` asserts completion;
`ShouldHaveCompletedStream(ClosedStreamScope)` asserts the exact owner and route;
`DecisionReadAdmissions.ShouldAdmitDecisionReadsIn(assembly)` checks projection
admission in assembly-level specs.

## Sources

- [Stream decisions](https://github.com/Cratis/Arc/blob/main/Documentation/backend/csharp/chronicle/commands/stream-decisions.md)
- [Protected read-model decisions and limitations](https://github.com/Cratis/Arc/blob/main/Documentation/backend/csharp/chronicle/read-models/injecting-into-commands.md)
- [Concurrency and templates](https://github.com/Cratis/Arc/blob/main/Documentation/backend/csharp/chronicle/commands/concurrency.md)
- [Event tags](https://github.com/Cratis/Arc/blob/main/Documentation/backend/csharp/chronicle/commands/event-tags.md)
- [Completing streams](https://github.com/Cratis/Arc/blob/main/Documentation/backend/csharp/chronicle/commands/completing-streams.md)
