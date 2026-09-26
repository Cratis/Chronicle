---
title: Decision-consistent reads (.NET)
description: Guard an event-log append against changes to an event-source-keyed projection read for a decision.
---

A decision read folds a projection from the event log in a fresh session and supplies an opaque guard for an append. If an event that could affect the read model is appended for the same source after the read boundary, the guarded append reports a concurrency violation and appends nothing. An absent model is guarded too: a concurrent creation conflicts. An unrelated event type or a change for another source does not invalidate the read. This is optimistic concurrency, **not** a retry of the command or a global lock.

```csharp
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;

var read = await eventStore.GetDecisionReads().GetDetached<OrderEligibility>(orderId);
if (!read.Exists)
{
    // Decide whether creation is allowed, including the absent case.
}

var result = await eventStore.EventLog.AppendMany(
    [new EventForEventSourceId(orderId, new OrderPlaced())],
    guardedBy: [read]);
if (!result.IsSuccess)
{
    // A concurrency violation means the decision must be read again and resubmitted.
}
```

For a unit of work, `Get<T>(key)` enrolls into the ambient unit; `GetDetached<T>(key)` does not. Alternatively, enroll a detached read using `IUnitOfWork.AddDecisionRead`. A protected unit must be completed with its owner's capability (`UnitOfWork.ClaimDecisionReadCommitOwnership` and `CommitAsOwner`); calling `Commit()` from application code after enrollment is rejected. A protected commit with **no events** still validates the guard. A kernel too old to allow validate-only returns `DecisionReadValidateOnlyNotSupported`, never success. Units without decision reads keep their previous behavior. `IUnitOfWork.GetDecisionConflicts()` maps violated labels to the read model type and key without exposing boundaries.

## Admitted projections

Only a single Chronicle **projection** on the event log, keyed directly by the event source ID, with a nonempty finite set of event types (including removal types), can be protected. Empty keys, `*`, `#`, leading/trailing whitespace and event type IDs containing commas are refused. Routing keys must be empty or `$eventSourceId`; joins (including variants), children, nested projections, derivatives, event-property routing, all-event subscriptions, reducers and projections on other sequences are refused. String keys without a converting format are admitted. GUID keys, including GUID concepts, require lowercase canonical `D` form. **All writers of event source IDs for GUID-keyed models must use this same canonical form**; non-.NET writers using alternate casing or formatting are outside this guarantee even if the requested key is canonical. Numeric and other converting key types are refused. `IDecisionReads.Admit<T>()` checks the projection shape without I/O; `Get` also validates the key. Unsafe shapes and keys throw `DecisionReadRefused` with a typed reason before reading.

The client captures the unfiltered event-log boundary *before* folding and uses it for the guard. The filtered tail and a first-use client/kernel definition comparison are **diagnostics**, not certificates of executing-projection agreement. An incomplete fold detected by the probe refuses with `FoldIncomplete`; a listed-definition disagreement refuses with `DefinitionMismatch`. The executing projection definition and initialization state must agree with the client's definition. Deployments changing definitions, including cross-silo cache convergence, must quiesce protected commands. Multiple reads of one key merge their event types and use their earliest boundary; an explicit competing scope on that key is refused, while the decision scope replaces an ordinary append-time default scope. Targets must match the event store, namespace and event-log sequence.

## Limits and assumptions

The guarantee assumes a single ordered event-log writer, ordered primary storage reads, stable projection execution, and literal identity between source IDs and keys. Revise and redact do not advance the tail; migration generation replacement can bypass the grain; neither is guarded. Other event sequences, external data, clock values and definition changes are not guarded. A removal followed by recreation with nonempty defaults can fold differently from materialization even though the append guard still works. The fold replays the key's history, so its cost grows with that history.

The kernel's duplicate-sequence collision retry renumbers an append without revalidating scopes. Duplicate grain activations, foreign writers **or unresolved earlier storage outcomes** can therefore defeat this and existing concurrency scopes. Quiesce such writers; a separate kernel follow-up must address atomic failed batches, numbering resynchronization and compatibility with legacy scopes before altering that retry path.
