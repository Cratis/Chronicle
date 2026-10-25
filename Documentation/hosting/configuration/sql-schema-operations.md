---
description: Provision SQL schemas before starting Chronicle and verify versioned schema readiness without joining the cluster.
---
# SQL schema operations

Run migrations as a deployment step when kernel replicas must start without applying versioned DDL. Use the same Chronicle Server build and storage configuration for the migration job and the replicas. These operations support PostgreSQL, SQL Server, and SQLite.

## Provision before starting replicas

Run the server executable once with `--migrate-only`:

```sh
./Cratis.Chronicle.Server --migrate-only \
  --event-store orders --namespace production
```

This process does not start Orleans, cluster membership, or HTTP listeners. It uses the configured **Schema** connection identity, creates missing databases and schemas only when `Storage.CreateIfMissing` permits it, and applies migrations under Chronicle's existing per-database schema locks.

The operation visits:

- The cluster database.
- The System event store and its Default namespace.
- Every registered event store and namespace, plus Default for each store.
- Additional stores supplied with repeatable `--event-store` flags. Repeatable `--namespace` flags apply to each explicitly supplied store.
- Namespace and jobs migrations, the event-log, system, outbox and shared named-tag tables, namespace-registered sequences, inbox sequences derived from stored subscriptions, and existing sequence tables.
- Registered read-model tables and their projected columns, shadow and initialized-marker indexes, replay-promotion metadata, and declared indexes. Declared index builds finish before the command succeeds, using the configured DDL timeout.
- Unscoped unique-constraint tables from registered definitions, plus upgrades of existing scoped claim and values tables. Scope keys first encountered after provisioning still need runtime DDL.
- Existing replay and backup read-model tables. SQL read-model generations remain unsupported.

Explicit stores and namespaces are registered so subsequent kernel boots can discover them. Re-running the operation is supported. Applied migration identifiers unknown to the current build are reported but do not cause verification to fail.

For containers, set `Cratis__Chronicle__Storage__SchemaAction=MigrateAndExit` instead of passing `--migrate-only`. The command-line action takes precedence over the configured action.

## Verify readiness

Run a read-only schema probe:

```sh
./Cratis.Chronicle.Server --verify-schema \
  --event-store orders --namespace production
```

Or set `Cratis__Chronicle__Storage__SchemaAction=VerifyAndExit`. Verification reads history and table metadata; it does not register stores, acquire schema migration locks, create databases or schemas, or apply migrations. Dynamic table checks compare required columns, types and lengths, required nullability, and ordered primary keys. Migration-defined index checks compare uniqueness and ordered column keys; PostgreSQL also checks validity and readiness. Missing tables, columns and indexes are collected within each dynamic verification group.

Initialized-marker and shadow index readiness uses the same catalog checks as runtime reconciliation, not their generated names. These checks do not establish a full index-shape match on every provider. Declared read-model indexes are checked by generated name only: expression SQL, ordered keys, direction, uniqueness, method, collation, filters and included columns are not compared. A same-named but incompatible declared index can therefore pass verification. Keep declared indexes under Chronicle's control; this probe is not a complete schema-drift audit.

Sequence discovery uses the event-log, system and outbox, namespace registrations, stored subscriptions and existing sequence tables. For each subscription, Chronicle derives the target inbox name as `inbox-<source event store>`, using the same convention as runtime delivery, and visits it in every namespace of the subscribing store. Subscriptions registered after provisioning still need another provisioning run or runtime DDL.

After a successful migration job, configure normal replicas with:

```sh
export Cratis__Chronicle__Storage__SchemaMode=Verify
./Cratis.Chronicle.Server
```

`SchemaMode` defaults to `Migrate`, preserving migration-on-access behavior. `Verify` disables database and schema creation regardless of `CreateIfMissing`. Before accepting traffic, startup checks the cluster, System event-store, Default namespace and jobs schemas. Later versioned-schema accesses also fail if migrations are pending, including jobs initialization. Dynamic DDL still runs when needed. Newly encountered objects are logged once per process at Debug, with a Warning summary per flush giving the count and recommending `--migrate-only`. This is not a blanket DDL prohibition; runtime refusal is planned for the next major release.

Do not set `SchemaAction` on normal replicas: any non-`None` action exits instead of running the server. MongoDB supports these actions for [index provisioning and verification](mongodb-index-operations.md); its startup `Verify` check only warns and does not fail startup. InMemory schema actions remain successful no-ops, and normal startup warns when `SchemaMode=Verify` is configured for InMemory.

## Exit codes

| Code | Meaning |
| --- | --- |
| `0` | Migration completed, verification is current, or storage has no versioned schema. |
| `2` | Verification found a missing database, schema, required dynamic object, incompatible column or index, or pending migration. |
| `3` | A database schema lock could not be acquired before its timeout. |
| `4` | Configuration is invalid, or migration needs a database that creation policy does not permit Chronicle to create. |
| `1` | An unexpected error occurred. |

CI checks that previously passed can now exit `2` for missing or incompatible dynamic objects. Run `--migrate-only` with the upgrading build before running `--verify-schema`.

Structured logs identify the logical scope, database, pending migrations, elapsed time and final exit code. Correct the reported configuration or run the migration job before retrying; do not treat a timeout as success.

## Kubernetes deployment

Use a Job ahead of the replica rollout. The referenced secrets must contain the appropriate `Cratis__Chronicle__Storage__*` environment variables. Use the same pinned server image for both resources; the image below illustrates the configuration.

```yaml
apiVersion: batch/v1
kind: Job
metadata:
  name: chronicle-migrate
spec:
  backoffLimit: 0
  template:
    spec:
      restartPolicy: Never
      containers:
        - name: migrate
          image: cratis/chronicle:latest
          args: ["--migrate-only", "--event-store", "orders", "--namespace", "production"]
          envFrom:
            - secretRef:
                name: chronicle-schema-storage
```

Wait for this Job to complete successfully before rolling out replicas. A failed or timed-out Job must block the rollout. Each server container then uses the runtime storage configuration and Verify policy:

```yaml
envFrom:
  - secretRef:
      name: chronicle-runtime-storage
env:
  - name: Cratis__Chronicle__Storage__SchemaMode
    value: Verify
  - name: Cratis__Chronicle__Storage__SchemaAction
    value: None
```

The runtime identity still needs permissions for Chronicle's dynamic per-table operations. In Verify mode, these tables are still created when first used:

- Event-sequence tables for sequences that did not exist when `--migrate-only` ran.
- Scoped unique-constraint tables for scope keys not yet encountered.
- Read-model tables and indexes registered after provisioning, including replay tables first needed by a later rebuild.

Run provisioning again when registrations change. Versioned schema objects still come from migrations, which `--migrate-only` applies and Verify checks.

Replicas from the previous release keep running against the migrated schema during a rollout. They ignore migrations they do not know, so a rollout stays safe as long as each release only adds tables, columns and indexes. Keep schema connection resolution consistent with the [storage connection resolution configuration](storage-connection-resolution.md).
