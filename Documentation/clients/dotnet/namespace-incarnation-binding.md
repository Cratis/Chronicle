---
title: Cratis Chronicle .NET namespace incarnation binding
description: Bind .NET writes and units of work before decision reads so stale decisions cannot write to a recreated Cratis Chronicle namespace.
---

Use a Cratis Chronicle .NET incarnation-bound writer when a decision must not cross removal and recreation of a named namespace. Capture its binding **before the reads that determine the event**, then append with that captured identity. Resolving an event store by namespace name alone does not do this.

## Prerequisites

- A kernel with `ChronicleOptions.Features.NamespaceIncarnations` enabled after a compatible rollout, and a storage provider supporting protected appends. See [Namespace incarnation fence](../../namespaces/incarnation-fence.md).
- A .NET client exposing `INamespaceBindings` and `INamespaceBoundEventSequences`. These are separate capabilities; the existing `IEventStore` and `IEventSequence` interfaces have not gained binding members.
- An already selected and initialized event-store instance whose event-store name and namespace match the canonical binding target. Normal Read and Append authorization and event-type registration still apply.

## Capture, decide, append

The following helper accepts that event-store instance, an event-source id, and a callback that reads application state and returns a registered event. The callback is deliberately invoked only after the binding has been captured and the bound writer constructed. This is an immediate append example, not a unit-of-work or read-set-concurrency implementation.

```csharp
using System;
using System.Threading.Tasks;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Namespaces;

public static class NamespaceBoundWrites
{
    public static async Task<AppendResult> DecideAndAppend(
        IEventStore eventStore,
        EventSourceId sourceId,
        Func<Task<object>> decide)
    {
        if (eventStore is not INamespaceBindings bindings ||
            eventStore is not INamespaceBoundEventSequences sequences)
        {
            throw new NamespaceBindingsNotSupported();
        }

        var binding = await bindings.LookupNamespaceBinding(eventStore.Namespace);
        var writer = sequences.GetEventSequence(EventSequenceId.Log, binding);
        var decidedEvent = await decide();
        return await writer.Append(sourceId, decidedEvent);
    }
}
```

Inspect the returned `AppendResult` as for an ordinary append; returning from the transport is not enough to establish success. Normal constraints and concurrency scopes are still needed where the business decision requires them.

The lookup is read-only: it does not Ensure, create, or reopen a namespace. The binding contains the canonical target, status, protocol version, and an identity only while Open. A non-open snapshot cannot create a bound writer, so the helper never invokes its decision callback for that snapshot.

The factory creates a separate bound writer without binding the event store's ordinary cached writer. Continue using the returned writer: looking up a binding and then appending through an old `eventStore.EventLog` reference does not enroll that reference.

The bound writer retains its identity across supported single, batch, named-tag, keyed, and effect-receipt append routes. Binding does not imply support for every append feature: for example, keyed appends still require the kernel's idempotent-append capability. Unsupported operations must fail rather than select a legacy route.

## Bind a unit of work before its reads

For a unit of work, use the separate `Cratis.Chronicle.Transactions.INamespaceBoundUnitOfWork` capability:

1. Capture the namespace binding before decision reads, as above.
2. Begin a unit through `eventStore.UnitOfWorkManager.Begin(correlationId)`.
3. Require that the unit implements `INamespaceBoundUnitOfWork`, then call `BindNamespace(binding)` **before enrolling decision reads or staging events**. If it does not implement the capability, fail closed.
4. Stage the decided events through the bound writer's `Transactional` sequence and commit the unit normally. Do not perform an immediate append inside a protected decision-read region.

Repeating the same binding is allowed; replacing it is not. A different target, non-open snapshot, late first enrollment, or changed incarnation is refused. Transactional staging propagates the bound writer's identity into a compatible unit, but does not retroactively protect reads made before that identity was captured.

A failed commit or rollback does not authorize rebinding the old work. If the application intentionally starts work for a replacement namespace, create a fresh operation/unit, capture a new binding, and repeat the decision reads. Never change only the binding while retaining the old decision or staged events.

## Handle refusals

- `NamespaceBindingsNotSupported` means a required client/kernel capability or binding protocol is unsupported, including an older kernel returning Unimplemented. Upgrade the compatible components or reject the operation; do not silently retry through an unbound writer.
- `NamespaceBindingNotValid` means the snapshot cannot enroll this writer/unit, including wrong target, missing identity, non-open status, or attempted replacement. Correct enrollment order/target selection. It is not permission to refresh a stale business decision.
- Kernel failures such as `NamespaceIncarnationMismatch`, `NamespaceIncarnationClosed`, and `NamespaceRecoveryRequired` require the actions in the [stable-code reference](../../namespaces/incarnation-fence.md#stable-failure-codes). A disabled or incompatible provider can report `NamespaceIncarnationUnavailable` rather than a client-side capability exception.
- `NamespaceRecoveryRequired` is terminal for the client's automatic namespace-registration retry loop. Surface the failure to the operator workflow rather than adding an unbounded retry wrapper.

Do not classify errors by fragments of exception text or assume new HTTP 409/503 behavior. The [phase-1 boundaries](../../namespaces/incarnation-fence.md#phase-1-boundaries) distinguish Core/gRPC codes from existing HTTP status/redaction behavior.

## Keep retry and deadline boundaries explicit

Protected storage work shares at most three attempts, with cancellable 25–100 ms jitter only on allowed retries. Receipt-collision recovery shares that budget; it does not recapture the namespace.

An explicit storage-layer `NamespaceCommitContext.Deadline` is honored when supplied by that caller. There is no new deadline parameter on `IEventSequence.Append`, and an HTTP/gRPC deadline is **not automatically propagated** into the storage context. Do not infer a new end-to-end timeout from opting into the binding. See [Retries and deadlines](../../namespaces/incarnation-fence.md#retries-and-deadlines), including how an unknown submitted commit differs from pre-commit cancellation.
