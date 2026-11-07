---
title: Provision and verify Cratis Chronicle MongoDB indexes
description: Provision Chronicle MongoDB indexes before deployment and verify them without starting the Kernel, using the configured schema connection identity.
---
Chronicle's one-shot schema commands provision MongoDB indexes before deployment or check them without starting the Kernel. They read the normal configuration files and environment variables but do not start Orleans, HTTP listeners, or observers. Use `--verify-schema` when a deployment must stop if required indexes are missing or incompatible; MongoDB's startup `Verify` mode only warns.

## Provision before deployment

Configure MongoDB storage and a schema identity with permission to read discovery data, list collections and indexes, create indexes, and remove obsolete indexes on the targeted databases. [Scope and permissions](#scope-and-permissions) explains connection routing and the jobs exception. Before provisioning a large event log, back up the deployment and review index-build capacity.

Run the server executable with `--migrate-only`:

```shell
./Cratis.Chronicle.Server --migrate-only
```

Chronicle creates missing named indexes, removes obsolete event indexes, and verifies the result. Repeating the command against a current deployment performs no index DDL (data definition operations such as creating or dropping an index). It does not drop collections, remove application data, or rebuild indexes whose names already exist.

An existing index with different keys or options is reported as incompatible and left untouched, with exit code `2`. Review the reported definition before manually dropping an incompatible index, then rerun `--migrate-only` with the schema identity.

Stored event stores, their stored namespaces, the system store, and default namespaces are discovered automatically. Add explicit scopes to provision a new store or tenant before it has registration data:

```shell
./Cratis.Chronicle.Server --migrate-only --event-store orders --namespace tenant-a
```

Repeat `--event-store` and `--namespace` to add scopes. Explicit namespaces apply to the explicitly requested stores. `--namespace` requires `--event-store`. Provisioning does not insert store, namespace, or event-sequence registration rows.

## Verify without DDL

Run a read-only check against the same configuration and explicit scopes used for provisioning:

```shell
./Cratis.Chronicle.Server --verify-schema --event-store orders --namespace tenant-a
```

Verification reads registration data, lists collection names, and lists indexes. Missing databases and collections are reported as missing indexes; the command does not create them. The report names each logical scope and physical database, then identifies missing, obsolete, or incompatible indexes and unclassified constraint collections. Unclassified collections produce a warning but do not cause exit code `2`; see [Scope and permissions](#scope-and-permissions) for how they are handled.

The comparison checks key fields and their order, key direction or type, uniqueness, sparseness, hidden state, time-to-live (TTL), partial filters, and collation. Simple collation is equivalent whether explicitly listed or omitted. Non-simple collation, including an inherited collection default, must match the expected definition. The legacy non-unique `value` index on per-event-source constraint collections is accepted.

| Exit code | Meaning |
| --- | --- |
| `0` | All classified required indexes are present with compatible keys and options and no obsolete indexes remain; unclassified collections are advisory |
| `1` | Unexpected failure, including MongoDB authorization or connection failures |
| `2` | Indexes are missing, obsolete, or incompatible |
| `3` | Schema lock could not be acquired; reserved for the shared SQL command contract |
| `4` | Configuration or operation validation failure |

Require exit code `0` before proceeding with a deployment that depends on these indexes. For code `2`, review the report and provision or correct the named indexes before checking again. A connection or authorization failure is not a successful verification.

Other exceptions follow the same exit-code mapping as [SQL schema operations](sql-schema-operations.md). MongoDB does not use SQL's schema lock.

## Startup behavior

The default `storage.schemaMode` is `Migrate`: runtime stores continue to create indexes lazily, as before. Offline provisioning is optional in this mode.

Set `storage.schemaMode` to `Verify` to add a read-only startup inspection limited to the `System` store, its `Default` namespace (including jobs), the event-store level, and the reserved global encryption scope. This startup path does not discover other stores or namespaces; `--verify-schema` retains full discovery:

```json
{
  "storage": {
    "type": "MongoDB",
    "connectionDetails": "mongodb://localhost:27017",
    "schemaMode": "Verify"
  }
}
```

MongoDB startup verification only warns about missing, obsolete, or incompatible indexes. Connection and authorization failures during this inspection also produce warnings rather than startup exceptions. Unlike SQL verification, it does not block startup.

Runtime index creation remains unchanged in both modes: stores create indexes on first access, so an index missing later can still require DDL privileges. On a fresh deployment, `Verify` warns about missing indexes until the Kernel or `--migrate-only` creates them. Use the one-shot `--verify-schema` command, not startup warnings, to block a deployment.

## Scope and permissions

The catalog covers the following scopes. Names below are unprefixed examples; a configured database-name prefix or connection resolver changes the physical destination.

| Database scope | Collections | Owned indexes |
| --- | --- | --- |
| Cluster (`chronicle`) | `event-stores` | Discovery only; document identity index |
| Event store (`orders+es`) | `namespaces`, `constraints` | Namespace discovery and current constraint definitions; document identity indexes |
| Event store (`orders+es`) | `occurrences` | Trigger/state and schedule/state |
| Namespace (`orders+es+Default`) | `event-sequences` | Discovery only; document identity index |
| Namespace | `event-log`, `system`, `outbox`, registered custom sequences | Event lookup/history, publication identity, subject |
| Namespace | `effect-receipts`, `<sequence>+closed_streams` | Occurrence lookup and closed scope/owner |
| Namespace | Existing `<sequence>+<constraint>+constraint` and `+values+constraint` | Value or owner lookup and sparse subject lookup |
| Namespace | `alert-incidents`, `failed-partitions`, `in-flight-events` | Alert browsing, observer and partition lookups |
| Namespace | `observer-definition-change-progress`, `observer-definition-change-continuations`, `observer-definition-change-restart-plans`, `observer-definition-change-inventory` | Observer/revision/identity lookups |
| Namespace | `occurrences`, `encryption-keys` | Occurrence delivery and encryption identifier/revision |
| Prefix-derived namespace on the jobs server | `jobs`, `job-steps`, `failed-job-steps`, `job-step-plans` | Status, type, creation order, job identity, plan incarnation, step order |
| Reserved global encryption scope | `encryption-keys` | Encryption identifier/revision |

The reserved global encryption scope is included even when no key has been written.

Uniqueness collections are discovered only when they already exist. Chronicle reads the event store's current stored constraint definitions and matches scoped or unscoped collection names using each definition's retention mode, rather than inferring mode from the `values` suffix. This distinguishes a per-event-source constraint literally named `values` from a per-value collection.

If no definition matches, or matching definitions disagree on mode, the collection is reported as unclassified. Verification warns but remains advisory for that collection. Migration performs no DDL on it and warns that runtime will create its indexes on first access. Provisioning never invents a collection from an absent constraint definition. A constraint first used after provisioning may still require runtime DDL. Legacy duplicate-value claim collections retain their non-unique value-index fallback.

The cluster registry, store namespace registries, and namespace event-sequence registries are read for discovery; they have no additional Chronicle-owned named indexes beyond their document identities. Application-owned MongoDB read-model indexes are outside this catalog.

### Choose identities for offline and runtime access

[Storage connection resolution](storage-connection-resolution.md) selects the `schema` purpose for offline Chronicle database connections. Startup verification uses the `runtime` purpose. Give the schema identity permission to read discovery data, list collections/indexes, create indexes, and remove obsolete indexes on all targeted databases. Verification needs discovery and index-listing permissions, but no index DDL permissions.

Jobs are an exception to namespace connection routing: the shared Orleans jobs adapter uses the MongoDB server in `storage.connectionDetails` and the prefix-derived namespace database name. Provisioning uses that same jobs client and database, even when Chronicle's namespace storage is redirected. Ensure the configured jobs identity can provision those indexes too.

Global encryption uses the same fixed logical store and namespace and the same connection-resolution path as runtime encryption. It is not inferred from the ordinary namespace list.

For example, run this `createRole` command on the `admin` database for one namespace, replacing `demo_orders+es+Default` with its resolved physical database name:

```json
{
  "createRole": "chronicle-runtime-namespace",
  "privileges": [
    {
      "resource": { "db": "demo_orders+es+Default", "collection": "" },
      "actions": [
        "find", "insert", "update", "remove", "createCollection",
        "listCollections", "listIndexes"
      ]
    }
  ],
  "roles": []
}
```

Apply equivalent data/discovery privileges to each targeted event-store, namespace, global encryption, and cluster database. This example deliberately grants neither `createIndex` nor `dropIndex`; it is not a complete role for every deployment.

### Retain permissions for runtime index creation

`Verify` only inspects indexes; it does not disable runtime DDL or guarantee that the whole Kernel can run without index privileges. Retain the index privileges required by runtime paths, including event-sequence fences and bindings, publication receipts, and constraints first used after provisioning.

The catalog does not provision indexes for the Orleans MongoDB provider's prefix-derived reminders collection (`chronicle.OrleansReminderV2`) or, with MongoDB clustering, its membership collection (`OrleansMembershipV3`). Retain the index privileges required by those provider paths on their collection resources.

MongoDB read-model databases also retain their existing index-creation requirements. Fence and binding indexes in read-model databases are created by Chronicle at runtime and are outside this catalog, which does not pre-provision them.
