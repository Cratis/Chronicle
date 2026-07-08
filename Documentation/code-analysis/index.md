# Code Analysis Rules

This section documents the code analysis rules provided by the Chronicle Code Analyzer for the .NET client.

All rules follow the identifier format `CHR####` where the numbers are sequential without gaps.

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
| [CHR0025](CHR0025) | Explicitly sourced read model property may be overwritten by AutoMap | Info | A property set with [SetFrom]/[SetValue]/… collides by name with another referenced event that AutoMap writes on top; add [NoAutoMap] or accept the update |

## Quick Fixes

All error rules (CHR0001, CHR0002, CHR0003, CHR0005, CHR0007) provide a code fix that automatically adds the `[EventType]` attribute to the referenced type.

## Installation

The analyzer is automatically included when you reference the `Cratis.Chronicle` NuGet package.
