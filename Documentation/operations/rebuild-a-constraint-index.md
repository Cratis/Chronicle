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

The generated gRPC equivalent is `Cratis.Chronicle.Contracts.Events.Constraints.Operator.ConstraintIndexes/ReindexConstraints`. Its request contains `EventStore`, `Namespace`, `EventSequenceId`, and `ConstraintNames`.

A successful command result returns the job identifier in `response`. That confirms the job was started, not that it finished. A job-start failure fails the command instead of claiming acceptance.

## Follow the job

Use the returned identifier in the Workbench Jobs view or the Jobs queries to follow progress and inspect failures. Governed appends are refused with `ConstraintIndexUnavailable` while their index is rebuilding; retry the unchanged append after the job and its handover finish. Unrelated appends can continue.

The replay reads only governed event type ids, across all stored generations. Unique constraints include claiming and removal types; closing constraints include closing, reopening, and redaction types. Missing governed schemas still fail the job. Missing schemas for unrelated types do not block it.

Unscoped unique indexes are cleared even when no governed history remains. A scoped unique index is cleared when governed history first encounters that scope; scopes without governed events remain untouched. Progress follows the last governed event processed. Clearing an index without processing an event leaves progress unknown so catch-up can establish it safely.

## Understand closing-constraint recovery

Explicitly rebuilding a closing constraint is retroactive: historical closing and reopening events determine its persisted closures. Merely registering that closing constraint remains non-retroactive. Rebuilding preserves manual closures and closures owned by other constraints.

See [Unique constraints](../constraints/declarative/unique.mdx#rebuilding-an-index) for claim recovery and [Constraints](../constraints/index.md#rebuilding-constraint-indexes) for the enforcement limits.
