---
title: Rebuild a Chronicle constraint index
description: Start and monitor an explicit unique or closing constraint index rebuild with ReindexConstraints, without changing the registered declaration.
---

Use `ReindexConstraints` to repair persisted claims or closures without editing and re-registering a declaration. The command starts the existing durable reindex job; it does not rewrite event history or change constraint definitions.

## Select the recovery scope

Choose the event store, namespace, and event sequence that hold the affected index. Supply the registered constraint names for a narrow repair, or use an empty `constraintNames` array to rebuild every applicable unique and closing constraint. Unknown names, unsupported constraint kinds, names that do not apply to the sequence, and an empty applicable selection are refused. An outstanding reindex job on that sequence also refuses a new request.

The command uses `AdministratorOperations`. On the System store and kernel scope, destructive-operation enforcement requires an administrator even while the broader operations policy is in audit mode. Ordinary event stores retain their own authorization rules. Authenticate through the kernel's normal operator session before making the request.

## Start the rebuild

Send an authenticated `POST` to `/api/events/constraints/operator/reindex-constraints`:

```json
{
  "eventStore": "example-store",
  "namespace": "default",
  "eventSequenceId": "event-log",
  "constraintNames": ["UniqueEmail"]
}
```

On an authenticated .NET connection, call `services.ConstraintIndexes.ReindexConstraints(request)`. The generated gRPC method is `Cratis.Chronicle.Contracts.Events.Constraints.Operator.ConstraintIndexes/ReindexConstraints`. Its request contains `EventStore`, `Namespace`, `EventSequenceId`, and `ConstraintNames`.

Check the command result for authorization, validation, and execution errors before reading `Response` (`response` over HTTP). A successful result returns a job ID, which confirms acceptance, not successful completion. If you don't need to wait, return that ID to the caller immediately.

## Follow the job

Use the returned ID in the Workbench Jobs view or poll `services.Jobs.AllJobs(new AllJobsRequest { EventStore = …, Namespace = … })` on the same authenticated connection. Check the query result and match the row whose `Id` equals the returned GUID. The high-level equivalent is `eventStore.Jobs.GetJob(id)` on the same store and namespace. Don't match by job type or details, or wait for all jobs to disappear: other jobs may be running concurrently.

- `CompletedSuccessfully` is the only confirmation of success. You don't need to observe `Running` first.
- `CompletedWithFailures` or `Failed` means the rebuild did not succeed. Inspect status changes and job steps for diagnostics. `Stopped` means it stopped and may be resumed; `Removing` means it is being removed. Neither is success.
- While a job is preparing, starting, or running, poll with a delay, cancellation, and a deadline. An initially missing row or transient query error can be retried within that bound. If the row remains missing, the query fails or access is denied, the job was deleted, or the wait times out, report the outcome as unavailable or indeterminate, not successful. Canceling the wait does not cancel the job.

Updated kernels retain successful reindex jobs just like failed ones, so a first poll after completion can still find the outcome. Normal retention applies to every reindex job, whether the operator, a changed constraint registration or an upgrade patch started it. Cleanup runs hourly by default and uses the job's creation time with a default retention of seven days, not seven days after completion. Configuration, a long-running job, or manual deletion can shorten the window. This is not an audit history. Older kernels may remove a successful job before the first poll even if they already expose the rebuild RPC; use both an updated .NET client and an updated kernel to wait reliably within normal retention.

Governed appends are refused with `ConstraintIndexUnavailable` while their index is rebuilding; retry the unchanged append after the job and its handover finish. Unrelated appends can continue.

The replay reads only governed event type ids, across all stored generations. Unique constraints include claiming and removal types; closing constraints include closing, reopening, and redaction types. Missing governed schemas still fail the job. Missing schemas for unrelated types do not block it.

Unscoped unique indexes are cleared even when no governed history remains. A scoped unique index is cleared when governed history first encounters that scope; scopes without governed events remain untouched. Progress follows the last governed event processed. Clearing an index without processing an event leaves progress unknown so catch-up can establish it safely.

## Understand closing-constraint recovery

Explicitly rebuilding a closing constraint is retroactive: historical closing and reopening events determine its persisted closures. Merely registering that closing constraint remains non-retroactive. Rebuilding preserves manual closures and closures owned by other constraints.

See [Unique constraints](../constraints/declarative/unique.mdx#rebuilding-an-index) for claim recovery and [Constraints](../constraints/index.md#rebuilding-constraint-indexes) for the enforcement limits.
