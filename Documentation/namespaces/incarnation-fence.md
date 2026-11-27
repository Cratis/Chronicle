---
title: Cratis Chronicle namespace incarnation fence
description: Fence named-namespace appends to a captured lifetime, handle stable errors, and understand recovery, retries, and phase-1 limits.
---

A Cratis Chronicle namespace name can be removed and reused. A decision made for the old namespace must not become a write to the replacement merely because the name is the same. The namespace incarnation fence lets a writer capture a kernel-issued identity **before its decision reads** and require that identity at commit.

This is an opt-in fence for named application namespaces. It is not a namespace authorization token, a read-set concurrency check, or a guarantee that every old client and administrative write path is fenced.

For a working client pattern, see [Bind a .NET writer to a namespace incarnation](../clients/dotnet/namespace-incarnation-binding.md). For interrupted removal, see [Recover a namespace removal](../operations/recover-a-namespace.md).

## Enable the coordinated mode

The kernel option `ChronicleOptions.Features.NamespaceIncarnations` defaults to `false`. Enable it only after every silo and namespace administration endpoint has upgraded and the storage provider supports supplied-token commit fencing. Configure the mode consistently across those upgraded endpoints. Mixed-version removal, mixed enabled/disabled administration paths, or rolling back to code that ignores the lifecycle records does not preserve this guarantee.

Protected appends require both the new kernel routes and a compatible storage provider. MongoDB requires a transaction-capable replica set or mongos; a standalone server is refused. SQL uses a transaction containing the namespace marker fence and event/receipt work. A missing provider capability or uninitialized marker is a refusal, not permission to use an unfenced write.

Namespace authorization remains separate:

| Operation | Required event-store capability |
| --- | --- |
| Look up an incarnation binding | Read: `chronicle.event-store.read` |
| Append through a bound writer | Append: `chronicle.event-store.append` |
| Inspect removal status, resume, or abort | Manage: `chronicle.event-store.manage` |

Read, Append, and Manage are independent grants. A grant for another event store does not cover the target store. **Configure store entitlement enforcement before exposing the operator APIs**: `authentication.enforceStoreEntitlements` is audit-only when `false`; the namespace feature does not enable authorization enforcement. See [Authentication configuration](../hosting/configuration/authentication.md).

## Capture the lifetime before deciding

An authorized binding lookup returns the canonical namespace name, lifecycle status, protocol version, and an incarnation identity only when the namespace is Open. Lookup does not Ensure, create, or reopen a namespace. An absent lifecycle rejects the lookup with `NamespaceIncarnationUnavailable`.

The non-open states are Preparing, Closing, Removed, and RecoveryRequired. None admits a bound writer. Do not generate an identity locally or treat a namespace spelling, creation timestamp, or cached event-store instance as an incarnation.

Capture and enroll the binding before reading the state used to decide an append. Keep that binding through single events, batches, named tags, idempotency keys, effect receipts, and unit-of-work commit. Looking up a binding alone does not enroll existing writers or units of work.

The provider checks the supplied incarnation before event or receipt work in the same transaction. This prevents a stale writer from succeeding merely because an idempotency key already has a receipt. It does not replace the append's normal concurrency scopes, constraints, or authorization checks.

If the namespace closes or is replaced after the reads, the old decision must fail closed. Do not refresh the binding and replay that decision into the replacement. A new lifetime requires a new, intentional operation with new decision reads.

## Stable failure codes

Recognized typed namespace failures are normalized to the following literal codes in Core/gRPC result-envelope `ExceptionMessages`. Check the envelope's authorization, validation, exception, and success fields; a transport response is not itself a successful operation. Unrelated failures are not converted into namespace failures by matching their messages or type names.

| Code | Meaning | Client or operator response |
| --- | --- | --- |
| `NamespaceClosing` | Removal has closed admission. | Stop issuing new decisions for this lifetime. Inspect removal status; do not recreate the namespace as a retry. |
| `NamespaceInitializing` | Initialization has not admitted the operation. | Wait with a bounded policy for initialization to complete before starting a fresh decision. Do not manufacture a binding. |
| `NamespaceRecoveryRequired` | Automatic lifecycle progress cannot safely continue. | Treat as terminal for automatic retries. Stop tenant work and use the authorized recovery workflow. |
| `NamespaceIncarnationMismatch` | The installed marker belongs to another lifetime. | Discard the old decision. Only explicit application intent may start a fresh operation against a newly captured binding. |
| `NamespaceIncarnationClosed` | The expected lifetime's marker is closed. | Do not replay the old decision or switch its identity. Inspect the lifecycle before deciding what new work is valid. |
| `NamespaceIncarnationUnavailable` | No admissible identity/provider capability is available, or coordination is disabled. | Check feature rollout, lifecycle initialization, and provider support. Never fall back to an unfenced append. |
| `NamespaceIncarnationRequiresReactivation` | The server activation was loaded for a different lifetime. | Server-side reactivation/reconciliation is needed. Keep the supplied binding unchanged; repeatedly acquiring a client writer is not a substitute. |
| `NamespaceWriteConflict` | A conditional lifecycle write or operator operation/revision precondition lost. | Re-read status and re-evaluate the action. Do not blindly substitute a newer revision or operation id. |
| `NamespaceRemovalCannotBeAborted` | Aborting would risk destructive or unresolved work. | Leave admission closed. Investigate the prior outcome; do not force the old identity open. |

A lookup's non-open status is also a refusal to enroll; not every such snapshot is returned as an exception code. The .NET client additionally uses `NamespaceBindingsNotSupported` for missing/unsupported binding support and `NamespaceBindingNotValid` for invalid enrollment. See the [client guide](../clients/dotnet/namespace-incarnation-binding.md#handle-refusals).

`NamespaceRecoveryRequired` stops the .NET client's automatic namespace-registration retry loop. Do not wrap that terminal result in an application policy that retries indefinitely.

**HTTP behavior is unchanged.** Phase 1 adds no namespace-specific HTTP status mapping, including no new promise of 409 or 503 responses. Existing HTTP result handling and exception-detail redaction still apply. The stable-code contract above is for Core/gRPC envelopes; do not bypass HTTP redaction or infer a namespace state from HTTP status alone.

## Retries and deadlines

Protected storage transactions share one budget of **three total attempts**, including the initial transaction and receipt-collision recovery. Only definite aborted transaction conflicts are retried. An allowed retry waits **25–100 ms** of cancellable jitter; the first attempt is not delayed. Collision resolution does not receive a new budget or namespace identity.

A storage caller may supply the absolute `NamespaceCommitContext.Deadline`. It is linked to activation cancellation, checked around backoff and transaction/commit boundaries, and never renewed by a retry. Without an explicit deadline, the existing activation token remains in effect. This phase does not invent a new default timeout, add a high-level .NET append deadline argument, or automatically propagate an HTTP/gRPC deadline into that storage context.

Cancellation or deadline expiry before commit submission refuses the uncommitted work. After submission, loss of the acknowledgement remains an **unknown commit outcome**: it is not a definite abort and must not trigger an automatic transaction retry. Conversely, an actual commit acknowledgement remains a known success even if it arrives after the deadline. A timeout alone is not proof that nothing committed.

Namespace removal has a separate persisted recovery budget: at most **five automatic attempts per budget**, a **30-second whole-attempt observation window**, and a **one-minute retry delay** for eligible failures. These are not per-checkpoint timeout renewals. Activation changes do not reset the consumed budget. A timeout does not cancel the underlying provider effect; unresolved work moves to RecoveryRequired instead of being issued again. An authorized resume can grant a fresh budget only when its safety preconditions hold.

## Phase-1 boundaries

- The client binding/writer and unit-of-work opt-in described here is available in the .NET client. Do not infer equivalent opt-in helpers in other clients from the shared wire contracts.
- Ordinary cached writers and legacy interfaces remain available. They do not acquire this decision-to-lifetime binding merely because a lookup occurred, and must not be an automatic fallback from a protected operation.
- The guarantee is for the supported named-namespace append/receipt paths. It does not extend to imports, bulk/administrative mutation paths, default/System-store administration, or arbitrary custom providers.
- The fence is not read-set isolation or a promise of complete physical erasure. Provider cleanup is provider-specific; application-owned read models and any retained data require separate handling. A Removed lifecycle is not evidence that all external data was deleted.
- Resume is not a force-recovery API. There is no phase-1 option to clear an unknown destructive outcome or reopen a retired identity based only on backend health or elapsed time.
- New Core/gRPC errors do not change HTTP status/redaction rules or establish automatic RPC-deadline propagation.
