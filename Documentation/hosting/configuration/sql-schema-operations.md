---
description: Provision Chronicle SQL schemas as a deployment step, check versioned readiness without joining the cluster, and interpret migration exit codes.
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
- Namespace and jobs migrations, the event-log, system, outbox and shared named-tag tables, and existing sequence tables, including inboxes.
- Read-model databases, without creating arbitrary read-model tables.
- The `chronicle_sink_cycle_receipts` table (and, if only that is missing, its `CreatedAt` index) in each read-model database. Multi-write SQL sink cycles use it to commit atomically.

If the receipt table has drifted unsafely (for example a changed column or primary key), `--migrate-only` refuses to provision it and does not repair it. Fix the table by hand and run the operation again.

Explicit stores and namespaces are registered so subsequent kernel boots can discover them. Re-running the operation is supported. Applied migration identifiers unknown to the current build are reported but do not cause verification to fail.

For containers, set `Cratis__Chronicle__Storage__SchemaAction=MigrateAndExit` instead of passing `--migrate-only`. The command-line action takes precedence over the configured action.

## Verify readiness

Run a read-only schema probe:

```sh
./Cratis.Chronicle.Server --verify-schema \
  --event-store orders --namespace production
```

Or set `Cratis__Chronicle__Storage__SchemaAction=VerifyAndExit`. Verification reads history and table metadata; it does not register stores, acquire schema migration locks, create databases or schemas, or apply migrations. It checks versioned EF schemas, the presence of well-known sequence tables and the `chronicle_sink_cycle_receipts` table with its `CreatedAt` index. It does not check future dynamically shaped read-model or constraint tables. A missing or drifted receipt table fails with `SchemaNotCurrent`, and verification never creates or repairs it.

After a successful migration job, configure normal replicas with:

```sh
export Cratis__Chronicle__Storage__SchemaMode=Verify
./Cratis.Chronicle.Server
```

`SchemaMode` defaults to `Migrate`, preserving migration-on-access behavior. `Verify` disables database and schema creation regardless of `CreateIfMissing`. Before accepting traffic, startup checks the cluster, System event-store, Default namespace and jobs schemas. Later versioned-schema accesses also fail if migrations are pending, including jobs initialization. Dynamically shaped tables still use their existing per-table path; this is not a blanket DDL prohibition.

Do not set `SchemaAction` on normal replicas: any non-`None` action exits instead of running the server. MongoDB supports these actions for [index provisioning and verification](mongodb-index-operations.md); its startup `Verify` check only warns and does not fail startup. InMemory schema actions remain successful no-ops, and normal startup warns when `SchemaMode=Verify` is configured for InMemory.

## Exit codes

| Code | Meaning |
| --- | --- |
| `0` | Migration completed, verification is current, or storage has no versioned schema. |
| `2` | Verification found a missing database, schema, required table or pending migration. |
| `3` | A database schema lock could not be acquired before its timeout. |
| `4` | Configuration is invalid, or migration needs a database that creation policy does not permit Chronicle to create. |
| `1` | An unexpected error occurred. |

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
- Unique-constraint tables.
- Read-model tables, including the per-generation tables created when a projection is rebuilt.

The `chronicle_sink_cycle_receipts` table is not created in Verify mode. Run `--migrate-only` first, including after an upgrade. A Verify replica now fails at startup with `SchemaNotCurrent` if the receipt table or its required index is missing or drifted in any known event store and namespace, including the system store and default namespaces. It does not report ready or repair the table. `--verify-schema` checks the same schema. Multi-write cycles still fail clearly with `SchemaNotCurrent` if the receipt schema is missing or drifted when accessed at runtime.

Two upgrade rules follow from this:

- Drop the `chronicle_sink_cycle_receipts` table only while no silo running the new version is using the database. A running silo can lose a cycle's receipt, which it needs to tell whether a retried cycle already committed.
- A namespace created after `--migrate-only` ran has no receipt table. Run `--migrate-only` again for it (`--event-store` and `--namespace`) before Verify replicas serve its multi-write cycles.

### Receipt cost and retry budget

Every multi-write cycle whose execution strategy has `RetriesOnFailure` enabled inserts a receipt on its **first attempt**, even if no retry occurs. The first attempt skips the receipt probe but pays for the receipt write, its index maintenance and amortized cleanup. Subsequent attempts probe before reapplying writes. SQLite's default strategy does not retry and writes no receipts; it still needs the receipt schema in Verify mode.

The five-minute limit is the verification window for retries **within one call**, measured on the database clock, not the receipt's storage lifetime. Keep the total execution-strategy retry budget, including command execution and delays, within that window. When a retry finds no receipt after the window, it throws `SinkCycleOutcomeUnknown` rather than applying the cycle again. The operator sees an Error log identifying the read model, sequence number and receipt id: the outcome is unknown and too old to verify, and the cycle is not applied again. Investigate the committed read-model state and decide on a replay instead of blindly retrying the write.

This is not deduplication across redelivery. A redelivered cycle starts a new call with a new receipt id; it can apply the writes again, even if the previous call's receipt remains. Expired or deleted receipts cannot establish that an earlier delivery committed, and a new call does not emit the old call's outcome-unknown Error merely because it was redelivered. Finished receipts are cleaned up in batches; receipts left by crashed owners are swept after 24 hours. Do not use that retention period as a redelivery or exactly-once guarantee.

Every other schema object comes from versioned migrations, which `--migrate-only` applies and Verify checks.

Replicas from the previous release keep running against the migrated schema during a rollout. They ignore migrations they do not know, so a rollout stays safe as long as each release only adds tables, columns and indexes. Keep schema connection resolution consistent with the [storage connection resolution configuration](storage-connection-resolution.md).
