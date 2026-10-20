---
title: Roll out a new version to a multi-node Chronicle cluster on SQL
description: Replace the nodes of a Chronicle kernel cluster on Azure SQL or Azure Database for PostgreSQL one at a time, know what each starting node does to the database, and see which guarantees Chronicle does not give you.
---

This guide shows an operator how to replace the nodes of a Chronicle kernel cluster that uses SQL clustering, one node at a time, when the new version may change the database schema. You finish with a rollout order, probe settings and a list of things to watch while it runs.

It covers Azure SQL (`MsSql`) and Azure Database for PostgreSQL (`PostgreSql`) with `Cratis__Chronicle__Clustering__Type=Sql`. Chronicle makes no zero-downtime promise for a rollout. Read [What Chronicle does not provide](#what-chronicle-does-not-provide) before you rely on one.

## Prerequisites

- A running cluster set up as described in [Plan for availability](azure.md#plan-for-availability): `Sql` clustering on every node, one shared cluster database, and the same `clusterId` and `serviceId`.
- A way to replace nodes one at a time, such as a Kubernetes `Deployment` or an Azure Container Apps revision with controlled overlap.
- A backup of the database taken before the rollout. Chronicle can apply schema migrations when a new node starts, and it has no command to roll them back.

## Know what a starting node does

Every node runs the same startup. Nothing in it is specific to the first node, so the order of your rollout decides which node does the work:

1. **It migrates the cluster database before joining the cluster.** `ClusterDbContextMigrator` runs at the first silo lifecycle stage and prepares the cluster database, including the membership table that SQL clustering reads.
2. **It serializes schema changes with a database lock.** Migrations run under a lock held in the database itself: `sp_getapplock` on SQL Server and a `pg_advisory_lock` on PostgreSQL. A node that cannot get the lock within **5 minutes** fails with `DatabaseSchemaLockNotAcquired`. When several nodes start together, one migrates and the others wait for it, then find the schema current.
3. **It migrates event store and namespace databases on first access, not at startup.** The first request that touches a new event store, namespace or read model on any node runs its migration while the cluster is serving traffic.
4. **It runs `ChronicleServerStartupTask` once the silo is active.** The task registers event types, reactors and projections, and reactivates observers. Every node runs it. A step that fails because a sibling node has not settled is retried up to five times, waiting 2, 4, 8 and 16 seconds between attempts, on top of Orleans' own response timeout. Any other failure, or exhausting the retries, stops the host.

## Roll out the new version

### 1. Take a backup and read the release notes

Look for schema changes in the release notes. A release that only adds tables or columns is the easy case. A release that removes or renames a column, or tightens a constraint, is the one to plan, because nodes still running the old version keep using the schema the new node has already changed.

### 2. Upgrade one node first

Chronicle has no separate migration command or migrate-only mode. The first node that starts on the new version performs the migrations. Replace one node, and wait until it logs that it started successfully before replacing the next:

```text
Cratis Chronicle Server started successfully - ready and listening on port 35000 for gRPC (HTTP/2) and Workbench, API and OAuth (HTTP/1.1)
```

The server writes this line when the host has finished starting, which includes the startup task above. If the node fails instead, read the failure in step 6 before you continue.

### 3. Replace the remaining nodes one at a time

Allow one node to be unavailable at a time. In Kubernetes, set `maxUnavailable: 1` on the rolling update strategy and set `maxSurge` to a small number. Replacing nodes one at a time keeps the cluster membership changing slowly enough for the remaining silos to agree on who is alive.

During the rollout, nodes on the old version and on the new version run in the same Orleans cluster and call each other's grains. Chronicle does not configure Orleans grain versioning, so nothing in the kernel rejects or protects against a mismatched pair. Keep the window short, and do not leave a cluster in a mixed state overnight. See [What Chronicle does not provide](#what-chronicle-does-not-provide).

### 4. Give nodes time to start and stop

A node needs a startup time budget that covers the 5-minute schema lock wait and the startup task retries, because a node that waits on a migration is working, not hung.

- **Startup probe.** Use a generous startup probe and a restart only after it is exhausted. For example `periodSeconds: 10` with `failureThreshold: 60` allows 10 minutes. Do not start the liveness probe before the startup probe succeeds.
- **Liveness probe.** Keep its failure window longer than a schema lock wait, or the orchestrator restarts a node that is waiting on a migration and the next node then waits for the same lock again.
- **Shutdown.** The host allows 10 seconds for a graceful stop (`HostOptions.ShutdownTimeout`). Set the termination grace period at or above that and let the orchestrator send `SIGTERM`. The server treats a signal during startup as an orderly stop.
- **Defunct entries.** A node that restarts joins as a new silo and never reuses an old identity. Old entries stay in the membership table until the sweep removes them. `defunctSiloExpiration` defaults to three hours and `defunctSiloCleanupPeriod` to one hour. They only control cleanup of dead entries, so do not raise them to make a rollout safer. See [Clustering](configuration/clustering.md).

Orleans' own failure detection settings, such as probe timeouts and the number of missed probes, are not exposed by Chronicle's configuration. Chronicle sets only the defunct-silo cleanup and the response timeout. If a node restarts slowly, raise `clustering.responseTimeout` (`Cratis__Chronicle__Clustering__ResponseTimeout`, default 30 seconds), which governs how long the startup grain calls wait.

### 5. Probe the right thing

The `/health` endpoint on the main or dedicated [health port](configuration/health-endpoint.md) answers from a single check: it connects to the cluster database with the runtime identity and runs `SELECT 1`, with a three-second timeout. Use it for liveness and readiness, with these limits:

- It does **not** report whether the startup task has finished.
- It does **not** report whether the node has joined the cluster membership.
- It does **not** cover the event store and namespace databases.

Chronicle has no separate startup or readiness endpoint. The probes above tell you that storage is reachable. To know a node is ready, watch for the `started successfully` line in step 2, and check that the node is in the membership table before you replace the next one.

### 6. Handle a node that does not come up

- **`DatabaseSchemaLockNotAcquired`.** Another node held the schema lock for more than 5 minutes. Look for a migration that is still running, or a node that crashed while holding it, and let the orchestrator restart the waiting node. Do not start more nodes meanwhile.
- **`DatabaseMigrationIncomplete`.** A concurrent run hit an already-exists error and migrations are still pending. Restart the node. If it repeats, a person has to look at the database, since a duplicate object is in the way.
- **A startup step reports exhausted retries.** The node could not reach its siblings through five attempts. Check that ports 11111 and 30000 are reachable between nodes, and that no other node is mid-restart.

If a new version cannot run against the migrated schema, restore the backup from step 1 and start the previous version. Chronicle does not downgrade a schema.

## Run without database creation rights

Set `Cratis__Chronicle__Storage__CreateIfMissing=false` when a platform provisions databases and schemas. A missing database fails with `DatabaseDoesNotExist` and a missing schema with `SchemaDoesNotExist`, each naming what to create.

This option stops Chronicle creating databases and schemas only. **Migrations still run in the existing databases**, so the identity Chronicle migrates as still needs rights to create and alter tables in its schema. The `Schema` and `Runtime` identities from [connection resolution](configuration/storage-connection-resolution.md#what-chronicle-asks) can be different logins, but both are used by the kernel process. Chronicle has no separate job that migrates the schema ahead of time, so you cannot run the kernel with only `db_datareader` and `db_datawriter` on its own tables.

Each new event store and each new namespace needs a database or schema provisioned ahead of its first use. Without it, the first access fails, and that access can happen after the rollout has finished.

## What Chronicle does not provide

- **No zero-downtime guarantee.** Orleans reactivates the grains of a stopped node on the remaining nodes, but Chronicle does not state how long that takes or what clients see meanwhile. Test a rollout against a copy of your data before relying on it.
- **No migration job and no migrate-only mode.** The nodes migrate.
- **No version gate.** Chronicle does not restrict how far apart two running versions may be, enforce expand-then-contract migrations, or limit migrations to the newest node. A change that removes or renames a column breaks nodes still running the old version. Plan those releases as a cluster stop and start, or keep each release additive.
- **No readiness check for membership or startup.** See step 5.
- **No schema downgrade.** Restore from backup.

## See also

- [Run Chronicle on Azure with Microsoft Entra ID](azure.md)
- [Clustering](configuration/clustering.md)
- [Health endpoint](configuration/health-endpoint.md)
- [Storage connection resolution](configuration/storage-connection-resolution.md)
- [Production deployment](production.md)
