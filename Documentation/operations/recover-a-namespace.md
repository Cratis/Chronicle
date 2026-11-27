---
title: Recover a Cratis Chronicle namespace removal
description: Inspect namespace removal status with Manage access, then conditionally resume or safely abort without reopening a retired identity.
---

Use this workflow when coordinated Cratis Chronicle namespace removal reaches RecoveryRequired or stops progressing. The goal is to resume only acknowledged-safe work, or abort before any destructive action, without reopening a retired namespace identity.

Do not use repeated removal requests, client reconnection, or namespace creation as a recovery loop. See the [namespace incarnation fence](../namespaces/incarnation-fence.md) for the contract and failure codes.

## Prerequisites

- Use a named application namespace in an event store with namespace incarnations enabled and compatible storage. This workflow is not default/System-store administration.
- Stop or drain application work for the affected tenant, including legacy/unbound clients. The opt-in fence is not a substitute for controlling unfenced writers.
- Use an authenticated operator caller of the generated Namespaces service. `GetNamespaceRemovalStatus`, `ResumeNamespaceRemoval`, and `AbortNamespaceRemoval` all declare **Manage** access on the target event store: `chronicle.event-store.manage`. Read access, Append access, or a Manage grant at another store is insufficient when entitlements are enforced.
- Enable `authentication.enforceStoreEntitlements` for enforcement; its default is audit-only. Apply the deployment's other authorization policies too. The namespace feature flag does not grant permissions or enable those policies. See [Authentication configuration](../hosting/configuration/authentication.md).
- Be able to investigate the storage provider's prior operation outcome. A healthy connection or elapsed timeout is not proof that an earlier destructive call has terminated.

## 1. Read the authorized status

Call `GetNamespaceRemovalStatus` with `EventStore` and `Namespace`. This reads persisted control state; it does not Ensure, adopt, create, or reopen a namespace. An uninitialized lifecycle is unavailable rather than initialized by inspection.

Check `IsAuthorized`, validation, exceptions, and `IsSuccess` on the query envelope **before using Data**. A denied result can contain an empty response object; default field values do not establish namespace existence or a valid operation.

The successful snapshot contains:

| Fields | Use |
| --- | --- |
| `Namespace`, `Status` | Canonical target and lifecycle state. |
| `OperationId`, `Revision` | The exact removal operation and revision required by conditional controls. |
| `Stage` | The next unacknowledged removal checkpoint. |
| `StageAttempts`, `TotalAttempts`, `BudgetStartAttempts` | Current checkpoint attempts and retained counts across operator-authorized budgets. |
| `AttemptInProgress`, `DestructiveAttemptOutstanding` | Whether an effect still lacks a terminal acknowledgement. |
| `DestructiveActionIssued` | Whether an irreversible checkpoint has been issued; abort is then unsafe. |
| `LastFailure` | A stable diagnostic, not permission to retry or a provider-completion proof. |

`OperationId` identifies the **removal operation**. It is not the namespace incarnation identity used by append writers. Do not manufacture either value.

Choose the next action from the state:

- **Open:** no recovery operation is eligible for these controls. New application work must capture its own Open binding before decision reads.
- **Preparing:** initialization is not complete. Do not treat it as an Open binding or try to force removal recovery through these controls.
- **Closing:** the owner/reminder is driving persisted checkpoints within its budget. Observe with a bounded policy; do not start a competing recovery action.
- **Removed:** the removal lifecycle completed. Verify provider-specific and application-owned cleanup separately; this is not a universal data-erasure assertion.
- **RecoveryRequired:** investigate the last issued checkpoint and outstanding-effect flags before selecting resume or abort.

Keep the observed `OperationId` and `Revision`. If either changes, re-evaluate the target and action rather than automatically replacing the request's preconditions.

## 2. Resume only after terminal reconciliation

Call `ResumeNamespaceRemoval` with `EventStore`, `Namespace`, the observed `OperationId`, and `ExpectedRevision` equal to the observed `Revision`.

The owner accepts it only for that exact operation/revision in RecoveryRequired, with **no attempt in progress and no unresolved destructive attempt**. It keeps the same namespace incarnation and removal operation, resumes from the persisted checkpoint, and authorizes a fresh bounded budget of five automatic attempts. The total historical attempt count is retained; it is not reset to zero.

Resume can continue after an acknowledged destructive checkpoint. That does not make abort safe: `DestructiveActionIssued` still prevents aborting the removal.

If an effect timed out or has an ambiguous outcome, do not assume that it aborted. Phase 1 has no force-resume or clear-unknown parameter. A resume with outstanding work continues to report `NamespaceRecoveryRequired`; backend health and a late response alone do not clear that persisted intent. Reconcile through a supported provider/recovery mechanism. If no mechanism can establish and persist a terminal outcome, leave admission closed and seek maintainer assistance rather than editing lifecycle rows or reissuing the effect.

## 3. Abort only before destructive work

Call `AbortNamespaceRemoval` with the same four request members and the exact observed operation/revision.

Abort also requires RecoveryRequired, and refuses if an attempt is in progress, a destructive outcome is outstanding, **or any destructive action has been issued**. A healthy backend does not relax these checks.

When safe, the owner acknowledges closing the old marker, reserves a **new incarnation**, and uses the normal initialization protocol to open that replacement. It never reopens the old identity and does not repeat the normal creation notification. Old writers and old decisions remain stale.

A failed abort is not evidence that the old lifetime reopened. Read status again before doing anything else. New clients may capture a new binding only after the replacement is Open, and must perform fresh decision reads.

## 4. Verify the result, not just the response

Resume and abort are acknowledgement-only commands: their response is the normal `CommandResult`, not a removal-status payload. A successful resume response is **not proof of completed deletion**. Query `GetNamespaceRemovalStatus` again and check the successful envelope before interpreting the snapshot. A resumed removal can still be Closing or later enter RecoveryRequired.

- `NamespaceWriteConflict` means the operation/revision/state precondition or conditional write changed. Read again and decide anew; do not blind-retry with fresh values.
- `NamespaceRemovalCannotBeAborted` means abort is unsafe. Do not switch to an unfenced path or force the old marker open.
- `NamespaceRecoveryRequired` is terminal for automatic recovery/reconnection retries until its cause is reconciled.
- A transport timeout is not a provider-completion acknowledgement. Preserve the uncertain outcome and inspect state before taking another action.

See [Stable failure codes](../namespaces/incarnation-fence.md#stable-failure-codes) for the remaining cases. There is no new namespace-specific HTTP status mapping in this phase; existing HTTP status and exception redaction rules remain in effect.

## API reference

These are Core/.NET contract member names on the generated Namespaces service (`Cratis.Chronicle.Contracts.Namespaces.INamespaces`), not new CLI commands or high-level `IEventStore` recovery methods.

| Operation | Request members | Successful envelope |
| --- | --- | --- |
| `GetNamespaceRemovalStatus` | `EventStore`, `Namespace` | `QueryResult<NamespaceRemovalStatusResponse>` containing the observed snapshot. |
| `ResumeNamespaceRemoval` | `EventStore`, `Namespace`, `OperationId`, `ExpectedRevision` | `CommandResult` acknowledging the request, not completed deletion. |
| `AbortNamespaceRemoval` | `EventStore`, `Namespace`, `OperationId`, `ExpectedRevision` | `CommandResult` after safe replacement's Open acknowledgement. |

The owner checkpoints are CloseMarker, Forget, Cleanup, Drop, DeleteListing, and Complete. The [removal retry budget and observation deadline](../namespaces/incarnation-fence.md#retries-and-deadlines) persist across activation changes. Neither an operator retry nor a new activation is permission to repeat an unresolved destructive checkpoint.
