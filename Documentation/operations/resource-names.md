---
title: Resource names
description: How Chronicle checks event store and namespace names at startup, what quarantine means, and how to recover from an overlap.
---

Event stores and namespaces become databases, collections and tables. Different names can end up in the same storage, for example when the storage compares names without regard to case. Chronicle checks for this so two names never share storage.

## The startup check

At startup the kernel reads the names already in use and groups the ones that would share storage. It does this before it accepts event store or namespace requests.

- While the check runs, requests that open an event store or namespace fail with `The server has not finished checking existing names. Retry shortly.` Retry after a short wait.
- If the check cannot read the names, the installation stays unavailable and the read is retried. `storage.nameAuditTimeout` (default five minutes) is the time budget; the kernel does not start when it runs out.
- If an overlap is found, `storage.nameCollisions` decides the outcome. See [storage](../hosting/configuration/storage.mdx#name-checks).

## Quarantine

With the default `Quarantine` handling, every owner of an overlap is made unavailable and the kernel starts. Opening a quarantined event store or namespace fails with `The event store or namespace is unavailable because its storage overlaps another one. See the server log.` Quarantined event stores and namespaces are left out of startup work and namespace listings. Names that do not overlap keep working.

A name that overlaps storage the kernel keeps for itself stops the kernel from starting, whatever the handling.

Log lines to look for:

| Level | Message |
| --- | --- |
| Warning | `The event store is unavailable because its storage overlaps the storage of another name` |
| Warning | `The namespace is unavailable because its storage overlaps the storage of another name` |
| Information | `The names in use were checked: ... event stores and ... namespaces are unavailable because of ... overlaps` |
| Critical | `The names in use could not be read, so the installation stays unavailable` |
| Critical | `A name overlaps storage the kernel keeps for itself` |
| Error | `Names overlap in storage` (with `Refuse` handling) |
| Warning | `A name was refused because ...` (a new name broke the rule, conflicted with an existing name or is quarantined) |

## Recover from an overlap

Recovery is offline. Chronicle does not merge or move storage on its own.

1. Find the overlapping names in the log.
2. Stop the kernels that use the cluster.
3. Decide which owner keeps the storage. Back up the storage of the other owner, then rename or remove the other event store or namespace outside Chronicle.
4. Start the kernels. The check runs again and the quarantine is lifted when no overlap remains.

## Upgrade patches and erasure while a name is unavailable

A quarantined event store cannot be opened, and a quarantined namespace is dropped from what its event store lists. Operations that visit every event store leave both out and log a warning for each (`Leaving out the quarantined event store ...`, `Leaving out the quarantined namespace ...`). This is what keeps the kernel starting.

- **Upgrade patches** (the ones that rebuild or clean constraint data in every event store): a patch that left an event store or namespace out is not recorded as applied, and the system version is not moved past it. It runs again at the next startup, once the quarantine is lifted, and then reaches what it missed. These patches are safe to repeat on stores they already reached.
- **Encryption key erasure for a subject**: the event stores that are not quarantined are erased as usual, but the erasure fails with an incomplete outcome and is not logged as done while a quarantined event store, or a quarantined namespace of the erased one, was left out. The log names what was not reached (`did not reach the ... quarantined event stores or namespaces`). Repeat the erasure after the quarantine is lifted. Until then a key kept there is not erased. Authorizing a new key behaves the same way.

## Reserved names

Names are reserved in the cluster storage when an event store or namespace is created: a `ResourceNameReservations` table in SQL, or a collection in MongoDB. A reservation is made after the name has passed the rule, and before any storage is created. If creation fails after the reservation, the reservation is kept and the retry reuses it.

To clean up a reservation for a name that was never created, remove its row or document from the cluster storage while the kernels are stopped.

## Forgetting is exact

Forgetting an event store or namespace only releases the exact name given. A name that differs only by case is a separate name and is not released.

## Re-creating a forgotten namespace

If a namespace is forgotten and later created again with the same name, Chronicle refuses to create it when storage for the earlier namespace is still present. Chronicle does not attach to storage it did not verify. To continue:

1. Verify what the old storage holds.
2. Back up the old storage and drop it: for MongoDB the read model database; for SQL the bound database and its binding row.
3. Or choose a new name.

## Order of checks

When an event store is opened, Chronicle checks in this order:

1. The startup snapshot, without any storage access. A name that is not ready or is quarantined is refused.
2. A read-only read of the cluster registry for a name not seen before.
3. The naming rule. A name that is already registered is accepted under its exact spelling; any other name that breaks the rule is refused.
4. The reservation, the registration and the creation of storage.

A refused name creates no reservation, no registration, no database and no collection.

## Next

- [Storage configuration](../hosting/configuration/storage.mdx#name-checks)
- [Upgrading: resource names](../upgrading/resource-names.md)
