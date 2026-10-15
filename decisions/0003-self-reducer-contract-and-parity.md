---
id: 0003-self-reducer-contract-and-parity
title: Use static self reducers with explicit state results and full reducer parity
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-10-11
decider: woksin
applies-to:
  - Source/Clients/DotNET/Reducers/**
  - Source/Clients/DotNET/Registrations/**
  - Source/Clients/DotNET/ReadModels/**
  - Source/Clients/Testing/**
  - Source/Clients/XUnit.Integration/**
---

## Context

A read model should be able to define its own reduction without a separate reducer class.
A separate creation method or mandatory default constructor would impose an unnecessary
lifecycle on that model. A smaller parallel reduction engine would also leave applications
choosing between concise declarations and the capabilities of existing reducers.

## Decision

A self reducer declares public static `Reduce(E, R? current[, EventContext])` methods on
its read model `R`, returning `R`, `R?`, `Task<R>`, or `Task<R?>`. Every handler explicitly
returns the next state; null represents deletion. No separate `Create` method or default
constructor is required. A new invoker adapter uses the existing reducer pipeline and
supports all existing reducer configuration, filtering, event-sequence selection,
external-store selection, tags, passive behavior, replay, generations, PII, read APIs,
and testing mechanisms. Dispatch uses the per-client event registry and selects the
most-specific unique handler; ambiguity fails rather than silently choosing a handler.
Registry-dependent caches cannot leak across clients. The classic `IReducerFor` API and
behavior remain unchanged, including its handling of methods returning plain `Task`.

Replay lifecycle hooks are independently optional public static methods returning exactly
`Task`: `BeginReplay()`, `EndReplay()`, `BeginReplayPartition(Partition)`, and
`EndReplayPartition(Partition)`. They may take trailing registered class/interface DI service
parameters resolved in the existing per-notification async scope; reduction itself accepts
no arbitrary service parameters. Hooks receive no event, event context, or current model.
No model is activated, and zero hooks means no activation or DI work. Duplicate same-name
hooks, generic methods, by-ref/optional/params arguments, and invalid return or parameter
shapes fail validation. Hooks are awaited before their scope is disposed. Ordering, failure
handling, and lifetime follow the existing notification machinery, with no guarantee of an
`End` notification after failure and no exactly-once guarantee. The classic instance-interface
notification path remains unchanged.

## Options considered

- **Static reduction with explicit state results:** chosen because creation, updates,
  deletion, and recreation use the same state-transition contract.
- **Separate `Create` and instance `Apply` methods, with default construction as a fallback:**
  rejected because they impose a second lifecycle and construction contract.
- **A reduced-feature self-reduction pipeline:** rejected because declaration style must
  not determine which reducer capabilities an application can use.
- **Changing classic reducers to require explicit state results:** rejected because this
  additive contract must not change existing applications' behavior.

## Default if unanswered

Applications continue using separate classic reducers. Introducing self reduction without
this decision risks incompatible creation rules or a permanent feature gap between the
new and existing forms.

## Timeline and scope

This contract applies before implementing or releasing self reduction. Implementation is
not yet delivered. The registry and explicit-registration foundation in
[Chronicle #4732](https://github.com/Cratis/Chronicle/issues/4732) and the related integration
work in [Arc #3163](https://github.com/Cratis/Arc/issues/3163) are prerequisites. The
behavior-preserving registry foundation does not implement self reduction.

Parity means the capabilities and semantics the shared engine actually provides, not
additional guarantees: inactive registration is not equivalent to model-level passive
on-demand reads; decision reads currently refuse reducers; observer stream-id filtering
is not supported. Existing passive-read metadata-filter gaps are tracked separately in
[Chronicle #4785](https://github.com/Cratis/Chronicle/issues/4785), not fixed by this record.
Replay lifecycle notifications currently instantiate classic reducers, and external-store
subscription inference scans instance handlers. The static lifecycle adaptation decided
above and registry-backed subscription inference require observable proof as part of
[Chronicle #4731](https://github.com/Cratis/Chronicle/issues/4731); a new invoker alone does
not establish lifecycle or subscription parity.

This record covers the self-reducer contract and parity only. Arc query endpoint selection
is owned and recorded by Arc, not decided here. Default-on marker-free discovery and changes
to classic reducer dispatch or return-value semantics are out of scope. Generation support
must remain consistent with [decision 0002](0002-event-generation-preservation-and-delivery.md).

## Verification

**Done when:** self reducers handle creation from null, updates, deletion, and recreation
through explicit synchronous or asynchronous state results; every existing reducer mechanism
works through the same pipeline; ambiguous handlers fail and separate clients never share
registry-dependent dispatch; classic reducers retain their existing API and behavior.

**Verify by:** focused client specifications for each accepted signature, null-state transitions,
most-specific dispatch, ambiguity, and two registries initialized in both orders; hook
signature rejection, optional hooks, scoped DI disposal, ordering, and failure behavior; parity
specifications and integration scenarios covering configuration, filtering, sequences,
external stores, tags, passive reads, replay, generations, PII, read APIs, and both Testing
and XUnit.Integration; run the existing classic-reducer regression suite unchanged.

## Consequences

Read models can own their transitions without requiring construction outside the handler.
The adapter must reuse the existing pipeline rather than duplicate it, and its release
requires full parity coverage. Plain `Task` is not a self-reducer result, but remains governed
by the existing classic-reducer contract.
