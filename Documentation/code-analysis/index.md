# Code Analysis Rules

This section documents the code analysis rules provided by the Chronicle Code Analyzer for the .NET client.

All rules follow the identifier format `CHR####`. Numbers are assigned sequentially; an occasional id is reserved for a rule still in progress, so a gap in the published set is expected.

## Rules Overview

| Rule ID | Title | Severity | Description |
|---------|-------|----------|-------------|
| [CHR0001](CHR0001) | Event type must have [EventType] attribute | Error | Types appended to event sequences must be marked with [EventType] attribute |
| [CHR0002](CHR0002) | Declarative projection event type must have [EventType] attribute | Error | Declarative projection generic arguments must reference types with [EventType] attribute |
| [CHR0003](CHR0003) | Model bound projection attribute must reference event type with [EventType] attribute | Error | Model bound projection attributes must reference types with [EventType] attribute |
| [CHR0004](CHR0004) | Reactor method has a parameter that cannot be resolved | Warning | Reactor method dependencies must be resolvable from event context, read models, or services |
| [CHR0005](CHR0005) | Reactor event parameter must have [EventType] attribute | Error | Event parameters in reactor methods must be marked with [EventType] attribute |
| [CHR0006](CHR0006) | Reducer method signature must match allowed signatures | Warning | Reducer methods must follow allowed signatures |
| [CHR0007](CHR0007) | Reducer event parameter must have [EventType] attribute | Error | Event parameters in reducer methods must be marked with [EventType] attribute |
| [CHR0012](CHR0012) | Event types should avoid nullable properties | Warning | Nullable properties are supported on events but are often better modeled as separate event types |
| [CHR0013](CHR0013) | Reactor cannot combine EventStore with explicit event sequence | Error | Reactors with [EventStore] must not also configure an explicit event sequence |
| [CHR0014](CHR0014) | Reducer cannot combine EventStore with explicit event sequence | Error | Reducers with [EventStore] must not also configure an explicit event sequence |
| [CHR0015](CHR0015) | Projection must not have side effects | Error | Projections must not inject ICommandPipeline or IEventLog |
| [CHR0016](CHR0016) | Projection Define() must not contain imperative code | Error | Projection Define() must only contain builder calls, not imperative statements |
| [CHR0017](CHR0017) | Constraint must not have side effects | Error | Constraints must not inject ICommandPipeline or IEventLog |
| [CHR0018](CHR0018) | Constraint Define() must not contain imperative code | Error | Constraint Define() must only contain builder calls, not imperative statements |
| [CHR0019](CHR0019) | Projection expression lambda must only access members | Error | Expression lambdas in projection builder methods must be simple member-access expressions |
| [CHR0020](CHR0020) | Constraint expression lambda must only access members | Error | Expression lambdas in constraint builder methods must be simple member-access expressions |
| [CHR0021](CHR0021) | Event types should be record types | Warning | Event types should be declared as record types for immutability |
| [CHR0022](CHR0022) | Reactor methods returning event side effects must be marked with [OnceOnly] | Warning | Reactor methods that return events must be [OnceOnly] to avoid appending duplicates during replay |
| [CHR0023](CHR0023) | Ambiguous parent key for [ChildrenFrom] collection | Warning | Parent-key inference is ambiguous when the child event has more than one property of the parent identifier type; specify parentKey |
| [CHR0024](CHR0024) | Read model property has no mapping source | Warning | A model-bound read model property has no mapping attribute and no subscribed event carries a same-named property, so AutoMap can never populate it |
| [CHR0025](CHR0025) | Explicitly sourced read model property may be overwritten by AutoMap | Info | A property set with [SetFrom]/[SetValue]/… collides by name with another referenced event that AutoMap writes on top; add [NoAutoMap] or accept the update |
| [CHR0026](CHR0026) | [Key] or [Subject] on an `EventSourceId<T>` is redundant | Warning | An `EventSourceId<T>`-derived property already is the key and compliance subject, so [Key]/[Subject] on it is redundant — remove the attribute |
| [CHR0027](CHR0027) | Ambiguous event stream id | Error | A type both implements ICanProvideEventStreamId and declares a non-null [EventStreamId]; this throws at startup |
| [CHR0028](CHR0028) | Redundant .AutoMap() call | Warning | .AutoMap() has no effect because AutoMap is enabled by default on projection builders; remove the call |
| [CHR0029](CHR0029) | Redundant .Set().To() with matching property names | Warning | A `.Set(x => x.P).To(e => e.P)` mapping with identical names duplicates what AutoMap already does; remove it |
| [CHR0030](CHR0030) | [ChildrenFrom] child collection property auto-maps to nothing | Warning | A [ChildrenFrom] child collection property matching no event property and with no explicit mapping always projects empty; rename it or bridge with `[SetFrom<T>]` |
| [CHR0031](CHR0031) | Reactor must not have mutable state | Warning | Reactors are re-created and replayed, so mutable instance state is unreliable; use readonly, primary-constructor-injected dependencies |
| [CHR0032](CHR0032) | Reactor must not access storage directly | Warning | Injecting a storage primitive like `IMongoCollection<T>` couples the reactor to a sink; read state via a read model or IReadModels.GetInstanceById |
| [CHR0034](CHR0034) | [PII] cannot be applied to an `EventSourceId<T>` | Error | The event source id is the encryption-key lookup identity, so it cannot be encrypted; [PII] on it throws PIINotSupportedOnEventSourceId at runtime |
| [CHR0035](CHR0035) | Read model declares a reserved '_subject' property | Error | Chronicle reserves the `_subject` field in a read model's document for internal compliance-subject tracking; a same-named property collides with it |
| [CHR0036](CHR0036) | Reducer must not have mutable state | Warning | Reducers are re-created and replayed, so mutable instance state or direct storage injection makes the fold non-deterministic; keep them stateless |
| [CHR0037](CHR0037) | Event type migration generations must share one explicit [EventType] id | Warning | The two generations referenced by an EventTypeMigration must carry the same explicit [EventType] id and differ only by generation, or the migration never applies |
| [CHR0038](CHR0038) | [Join] of a [PII] value crosses the compliance subject | Error | A join — model-bound or fluent — that copies a [PII] value from a stream keyed by something other than the read model's own subject cannot be decrypted and freezes the projection; it also puts that data beyond the owner's erasure |

## Quick Fixes

- CHR0001, CHR0002, CHR0003, CHR0005, and CHR0007 provide a code fix that adds the missing `[EventType]` attribute to the referenced type.
- CHR0026 provides a code fix that removes the redundant `[Key]`/`[Subject]` attribute.
- CHR0028 provides a code fix that removes the redundant `.AutoMap()` call.
- CHR0029 provides a code fix that removes the redundant `.Set(...).To(...)` mapping.

## Installation

The analyzer is automatically included when you reference the `Cratis.Chronicle` NuGet package.
