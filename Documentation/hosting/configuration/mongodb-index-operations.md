---
description: Provision and verify Chronicle's MongoDB indexes before deployment without starting the kernel, using the schema connection identity.
---
# MongoDB Index Operations

Use Chronicle's one-shot schema commands to provision MongoDB indexes before starting the kernel. These commands use the normal configuration files and environment variables, but do not start Orleans, HTTP listeners, or observers.

## Provision before deployment

Configure MongoDB storage, then run the server executable with `--migrate-only`:

```shell
./Cratis.Chronicle.Server --migrate-only
```

Chronicle creates missing named indexes, removes obsolete event indexes, and verifies the result. Repeating the command against a current deployment performs no index DDL. It does not drop collections, remove application data, or rebuild indexes whose names already exist. Back up the deployment and review index-build capacity before provisioning a large event log.

Stored event stores, their stored namespaces, the system store, and default namespaces are discovered automatically. Add explicit scopes to provision a new store or tenant before it has registration data:

```shell
./Cratis.Chronicle.Server --migrate-only --event-store orders --namespace tenant-a
```

Repeat `--event-store` and `--namespace` to add scopes. Explicit namespaces apply to the explicitly requested stores. `--namespace` requires `--event-store`. Provisioning does not insert store, namespace, or event-sequence registration rows.

## Verify without DDL

```shell
./Cratis.Chronicle.Server --verify-schema --event-store orders --namespace tenant-a
```

Verification only reads registration data, lists collection names, and lists indexes. Missing databases and collections are reported as missing indexes; verification does not create them. The report identifies each logical scope, its database name, missing or obsolete collection/index names, and unclassified constraint collections. Unclassified collections produce a warning but do not make the report non-current or cause exit code `2`. Verification checks index names, not a complete comparison of options and key definitions.

| Exit code | Meaning |
| --- | --- |
| `0` | All classified required indexes are present and no obsolete indexes remain; unclassified collections are advisory |
| `1` | Unexpected failure, including MongoDB authorization or connection failures |
| `2` | Indexes are missing or obsolete |
| `3` | Schema lock could not be acquired; reserved for the shared SQL command contract |
| `4` | Configuration or operation validation failure |

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

For MongoDB, startup verification **only warns** about missing or obsolete indexes. Connection and authorization failures during this inspection also produce warnings, not startup exceptions. Unlike SQL verification, this is not a fail-startup policy. Runtime lazy index creation remains unchanged in both modes; a missing index encountered later by a runtime store can still require DDL privileges. Use `--verify-schema` when a deployment gate needs a nonzero exit code.

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
| Prefix-derived namespace on the jobs server | `jobs`, `job-steps`, `failed-job-steps` | Status, type, creation order, job identity |
| Reserved global encryption scope | `encryption-keys` | Encryption identifier/revision |

The reserved global encryption scope is included even when no key has been written.

Uniqueness collections are discovered only when they already exist. Chronicle reads the event store's current stored constraint definitions and matches scoped or unscoped collection names using each definition's retention mode, rather than inferring mode from the `values` suffix. This distinguishes a per-event-source constraint literally named `values` from a per-value collection.

If no definition matches, or matching definitions disagree on mode, the collection is reported as unclassified. Verification warns but remains advisory for that collection. Migration performs no DDL on it and warns that runtime will create its indexes on first access. Provisioning never invents a collection from an absent constraint definition. A constraint first used after provisioning may still require runtime DDL. Legacy duplicate-value claim collections retain their non-unique value-index fallback.

The cluster registry, store namespace registries, and namespace event-sequence registries are read for discovery; they have no additional Chronicle-owned named indexes beyond their document identities. Application-owned MongoDB read-model indexes are outside this catalog.

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

For a restricted kernel runtime identity, start with data read/write and discovery permissions without `createIndex` or `dropIndex` on the kernel databases. The Orleans MongoDB provider still creates indexes unconditionally on the prefix-derived reminders collection (`chronicle.OrleansReminderV2`) and, with MongoDB clustering, the membership collection (`OrleansMembershipV3`). Grant the required index privileges on those collection resources. MongoDB read-model databases also retain their existing index-creation requirements; this catalog does not pre-provision their application-defined indexes.

## Acceptance coverage

Container-backed specifications migrate through the real server's one-shot command in an isolated process, compare the resulting index keys and options with runtime-created indexes, and exercise every runtime ensure path while counting index DDL commands. A separate authenticated-container specification verifies and writes with an identity denied index DDL, and checks that a newly introduced unscoped constraint fails with an actionable error. These specifications require container infrastructure and run in CI.
