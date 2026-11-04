---
title: Upgrading with resource name checks
description: What changes for an existing installation when Chronicle checks event store and namespace names, and how to roll it out.
---

Chronicle now checks event store and namespace names at startup and when they are created. See [Resource names](../operations/resource-names.md) for how the check behaves.

## What changes

- Names that already exist keep working under their exact spelling. Names that overlap in storage are quarantined by default, or stop startup when `storage.nameCollisions` is `Refuse`.
- A new cluster table or collection, `ResourceNameReservations`, records reserved names.
- Forgetting an event store or namespace now releases only the exact name. This is a compatibility change for callers that relied on a case-insensitive match.
- Re-creating a forgotten namespace fails when its old storage still exists. See [re-creating a forgotten namespace](../operations/resource-names.md#re-creating-a-forgotten-namespace) for the recovery.

## Roll out

1. Freeze creation of event stores and namespaces.
2. Do a rolling restart of the kernels to the new release.
3. Check the log for quarantine warnings and resolve them with [the recovery steps](../operations/resource-names.md#recover-from-an-overlap).
4. Reopen creation.

## SQL schema

With `SchemaMode=Verify`, run the migration first: the cluster migration `Cluster-ResourceNameReservations-v19_116_1` adds the reservations table, and verification fails until it has been applied. See [SQL schema operations](../hosting/configuration/sql-schema-operations.md).

## Downgrading

Older releases ignore the reservations table or collection. They do not detect name overlaps, and names created while running an older release are not reserved.
