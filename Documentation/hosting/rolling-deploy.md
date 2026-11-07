---
title: Roll out Chronicle upgrades to a multi-node cluster
description: Check Chronicle version compatibility, back up SQL storage and replace cluster nodes one at a time, using readiness probes to control traffic.
---

Upgrade a multi-node Chronicle cluster one node at a time only when the old and new releases can run together. Check compatibility before replacing a node, back up the databases and wait for each replacement to become ready before continuing. A rolling upgrade does not guarantee uninterrupted service.

The [compatibility check](#check-that-the-two-versions-can-share-a-cluster) applies to both `MongoDB` and `Sql` clustering. The SQL migration steps cover Azure SQL (`MsSql`) and Azure Database for PostgreSQL (`PostgreSql`) with `Cratis__Chronicle__Clustering__Type=Sql`, where a starting node can change the shared database schema. Review the [rollout limits](#what-chronicle-does-not-provide) when planning the upgrade.

## Prerequisites

- A running multi-node cluster, such as one set up as described in [Plan for availability](azure.md#plan-for-availability): the same clustering type on every node, one shared cluster database, and the same `clusterId` and `serviceId`.
- A way to replace nodes one at a time, such as a Kubernetes `Deployment` or an Azure Container Apps revision with controlled overlap.
- A backup of the database taken before the rollout. Chronicle can apply schema migrations when a new node starts, and it has no command to roll them back.

## Supported source releases

When upgrading from a version before 19.115.0, stop all nodes, or keep public client traffic off the cluster until every node runs 19.115.0.

Deployments that keep encryption keys in HashiCorp Vault or Azure Key Vault stop all nodes for the upgrade that gives new keys their own names, because an earlier node still writes keys under the earlier names.

A SQL Server deployment that keeps encryption keys in SQL stops all nodes for the release that compares key identifiers exactly, or applies it with `--migrate-only` first. See [encryption key identifiers on SQL Server](configuration/compliance-storage.md#encryption-key-identifiers-on-sql-server).

Rolling upgrades are supported from 19.111 onward, except that SQL Server storage requires 19.115.1 or later. Releases before 19.115 cannot connect to SQL Server, and 19.115.0 is listed as unusable in `.github/rolling-upgrade-unusable-releases.txt` because its administrator roster can deadlock on the Data Protection key ring (#5485). Upgrading from an older release needs a stop-all upgrade: stop every silo, then start the new release. Older releases lack the patch-manager handover or have changed grain interfaces that prevent the old and new silos from exchanging calls.

## Check that the two versions can share a cluster

While nodes are being replaced, nodes of the old version and of the new one form one Orleans cluster and call each other's grains. That only works when the new version still serves every grain call the old version makes. When it does not, the first new node stops during startup, and the rollout stalls with the old nodes still running.

Check the release notes for your source and target versions before using this table. Compatibility exceptions require a stop-all upgrade even within one major version. The current compatibility waiver names `v19.123.0`: job and job-step identifiers moved from `Cratis.Orleans` to `Cratis.Choreography`, changing grain wire identities. Let running jobs settle before upgrading from that release, then stop all nodes before starting the new version.

For releases without a compatibility exception, use this table:

| Upgrading | Within one major | How to upgrade |
| --- | --- | --- |
| From 19.111 or later | Yes | Replace nodes one at a time. Releases are checked in CI against the release before them. |
| From a 19.x release before 19.111 | Yes | Stop every node, then start the new version. Older releases lack the patch-manager handover or have incompatible grain calls. |
| From a release before 19.123.1 to 19.123.1 or later | No | Stop every node, then start the new version. The serializers were renamed from `Cratis.Orleans` to `Cratis.Choreography` in 19.123.1, and a node of either side cannot read a concept value written by the other. Replacing nodes one at a time crash-loops the new node and fills the old nodes with grain errors. A new node that finds such a release in the cluster logs a critical message and stops. |
| To a new major | No | Stop every node, then start the new version. A major release may change grain calls. |

To stop every node in Kubernetes, scale the deployment to zero and back, or use the `Recreate` strategy for that one upgrade. In Azure Container Apps, use single revision mode with no overlap. Stopping the old nodes first is safe; it is the overlap that fails.

A new node that meets an incompatible version logs a critical message naming the startup step and saying a full stop is needed, then stops:

```text
Startup step 'DiscoverAndRegisterKernelProjections' failed because a silo in this cluster most likely runs a Chronicle release this one cannot exchange grain calls with.
```

The exception beneath it is a `TypeLoadException` such as `Unable to resolve type alias "("inv",[Orleans.Runtime.GrainReference],[Cratis.Chronicle.Projections.IProjectionsManager,Cratis.Chronicle.Core],"F436C56F")"`. Versions before 19.107.0 show only the exception. Either way, roll back the new node, then stop every node and start the new version.

## Roll out administrator authorization

Keep `Authentication.EnforceAdministratorPolicyForOperations=false` on every node while replacing nodes
with the administrator-policy release. Existing operational calls other than System-store operations then remain in audit mode.
System-store reads and operations always require an administrator on upgraded nodes, regardless of this flag.
Nodes that have not been upgraded retain their earlier behaviour. The new
administrator-management service is always enforced on upgraded nodes when authentication is enabled;
old nodes do not expose that service.

After every node has been upgraded, review the would-deny warnings for user/application management,
and other administrator-policy operations. Confirm that the operators who need those operations
are active administrators, and obtain fresh user sessions carrying `chronicle_principal=user`. Refresh
OpenIddict user tokens or sign in again for existing cookie and Identity bearer sessions. Applications
cannot acquire administrator privilege, even if their token subject matches an administrator's identifier.

Only then set `Authentication.EnforceAdministratorPolicyForOperations=true` consistently across the
cluster. During a staggered configuration change, a call can still be audited rather than denied on a
node that retains audit mode. This does not apply to System-store operations, which are always enforced on upgraded nodes.
For other administrator-policy operations, enforcement is not cluster-wide until every node has the setting. Application-store
operations are unaffected. See [Administrator authorization](configuration/authentication.md#administrator-authorization)
for the configuration and external-authority limitations, and [The System event store](system-event-store.md) for recovery guidance.

Every release that includes the administrator roster must recognize `GrantReason.AdministratorGrant`
(value `3`) before administrator-management grants are written. A node whose enum converter does not
recognize a persisted reason cannot safely replay that history. Do not roll back to such a version after
writing those grants. A roster without active administrators must be recovered through the server-local
recovery configuration, not by disabling policy enforcement on a reachable node.

## Connection reservation in a mixed-version cluster

Updated nodes reserve a connection id across the cluster through a new grain. Nodes that have not been
updated neither host this grain nor call it, so a connection id opened on such a node is not part of the
reservation. Until every node is updated, the cluster-wide reservation is therefore incomplete, and each
node still applies its own local connection ownership. Once the last older node is replaced, the
reservation covers every connection. No action is needed beyond completing the rollout.

## Know what a starting node does

Every node runs the same startup sequence. In the default `SchemaMode=Migrate` mode, `ClusterDbContextMigrator` prepares the cluster database before the node joins, including the membership table used by SQL clustering.

Database locks serialize migrations: `sp_getapplock` on SQL Server and `pg_advisory_lock` on PostgreSQL. A node that cannot obtain the lock within five minutes fails with `DatabaseSchemaLockNotAcquired`. When several nodes start together, one migrates while the others wait, then check whether the schema is current.

Event store and namespace databases migrate on first access rather than all at startup. The first request that touches a database needing migration can therefore perform schema work while the cluster is serving traffic. To apply versioned migrations before that point, use the separate migration step described below.

Once the silo is active, `ChronicleServerStartupTask` registers event types, reactors and projections, and reactivates observers. Transient failures get up to five attempts, with waits of 2, 4, 8 and 16 seconds between attempts in addition to Orleans' response timeout. Most exhausted startup steps stop the host. Kernel projection registration can instead defer transient failures to background retries; incompatible-release failures still stop startup.

## Roll out the new version

### 1. Take a backup and read the release notes

Look for schema changes in the release notes. A release that only adds tables or columns is the easy case. A release that removes or renames a column, or tightens a constraint, is the one to plan, because nodes still running the old version keep using the schema the new node has already changed.

### 2. Upgrade one node first

By default (`SchemaMode=Migrate`), the first node that starts on the new version performs the migrations. To apply them as a separate deployment step instead, run the new version with `--migrate-only` before the rollout and start the nodes with `SchemaMode=Verify`; see [SQL schema operations](configuration/sql-schema-operations.md). Either way, replace one node, and wait until it logs that it started successfully before replacing the next:

```text
Cratis Chronicle Server started successfully - ready and listening on port 35000 for gRPC (HTTP/2) and Workbench, API and OAuth (HTTP/1.1)
```

The server writes this line when the host has finished starting, which includes the startup task above. If the node fails instead, read the failure in step 6 before you continue.

### 3. Replace the remaining nodes one at a time

Allow one node to be unavailable at a time. In Kubernetes, set `maxUnavailable: 1` on the rolling update strategy and set `maxSurge` to a small number. Replacing nodes one at a time keeps the cluster membership changing slowly enough for the remaining silos to agree on who is alive.

During the rollout, nodes on the old version and on the new version run in the same Orleans cluster and call each other's grains. When the [two versions can share a cluster](#check-that-the-two-versions-can-share-a-cluster), Orleans routes each call to a node that understands it, and a grain may move from an old node to a new one partway through the rollout. Keep the window short anyway, and do not leave a cluster in a mixed state overnight: a grain that moves is deactivated and reactivated, and every move costs time.

### 4. Give nodes time to start and stop

A node binds its listeners before starting its silo, so a schema lock wait or startup retries do not prevent startup and liveness probes from succeeding. Keep the node out of traffic until readiness succeeds.

- **Startup probe.** Use `tcpSocket` on port 35000 or an HTTP probe on `/health/live`. Raising `failureThreshold` is not the fix for backlog-dependent startup restarts; do not use readiness as the startup probe.
- **Liveness probe.** Use `/health/live`, which remains successful while the silo is starting, including while waiting on a schema lock.
- **Shutdown.** The Chronicle images forward `SIGTERM` to the kernel, which then leaves the cluster in an orderly way. The host allows 10 seconds for that graceful stop (`HostOptions.ShutdownTimeout`). Docker also waits only 10 seconds by default before killing the container, which leaves no margin, so give the container at least 30 seconds: `docker stop --time 30`, or `stop_grace_period: 30s` in Docker Compose. In Kubernetes, keep `terminationGracePeriodSeconds` at 30 seconds or more (30 is the default). The server treats a signal during startup as an orderly stop.
- **Defunct entries.** A node that restarts joins as a new silo and never reuses an old identity. Old entries stay in the membership table until the sweep removes them. `defunctSiloExpiration` defaults to three hours and `defunctSiloCleanupPeriod` to one hour. They only control cleanup of dead entries, so do not raise them to make a rollout safer. See [Clustering](configuration/clustering.md).

Orleans' own failure detection settings, such as probe timeouts and the number of missed probes, are not exposed by Chronicle's configuration. Chronicle sets only the defunct-silo cleanup and the response timeout. If a node restarts slowly, raise `clustering.responseTimeout` (`Cratis__Chronicle__Clustering__ResponseTimeout`, default 30 seconds), which governs how long the startup grain calls wait.

### 5. Probe the right thing

Use `/health` (or `/health/ready`) on the main or dedicated [health port](configuration/health-endpoint.md) for readiness. It returns 503 until the silo startup task has finished and the host has started, and becomes unready when shutdown begins. It also runs existing storage health checks, including the cluster database check for SQL clustering; it does not cover every event store and namespace database.

Use `/health/live` for liveness, not `/health`. A TCP connection or successful liveness check means only that the listener is accepting requests. While unready, non-health traffic receives HTTP 503 with `Retry-After` or gRPC `Unavailable`, including Workbench traffic. Wait for readiness before replacing the next node; background catch-up may continue after readiness succeeds.

### 6. Handle a node that does not come up

- **`DatabaseSchemaLockNotAcquired`.** Another node held the schema lock for more than 5 minutes. Look for a migration that is still running, or a node that crashed while holding it, and let the orchestrator restart the waiting node. Do not start more nodes meanwhile.
- **`DatabaseMigrationIncomplete`.** A concurrent run hit an already-exists error and migrations are still pending. Restart the node. If it repeats, a person has to look at the database, since a duplicate object is in the way.
- **A startup step reports exhausted retries.** The node could not reach its siblings through five attempts. Check that ports 11111 and 30000 are reachable between nodes, and that no other node is mid-restart.
- **A startup step reports an incompatible release, or fails with `Unable to resolve type alias`.** The old and new versions cannot share a cluster. See [Check that the two versions can share a cluster](#check-that-the-two-versions-can-share-a-cluster).

If a new version cannot run against the migrated schema, restore the backup from step 1 and start the previous version. Chronicle does not downgrade a schema.

## Run without database creation rights

Set `Cratis__Chronicle__Storage__CreateIfMissing=false` when a platform provisions databases and schemas. A missing database fails with `DatabaseDoesNotExist` and a missing schema with `SchemaDoesNotExist`, each naming what to create.

This option stops Chronicle creating databases and schemas only. **Migrations still run in the existing databases**, so the identity Chronicle migrates as still needs rights to create and alter tables in its schema. The `Schema` and `Runtime` identities from [connection resolution](configuration/storage-connection-resolution.md#what-chronicle-asks) can be different logins. You can apply versioned migrations ahead of time with a separate [`--migrate-only` step](configuration/sql-schema-operations.md#provision-before-starting-replicas) that uses the `Schema` identity. The kernel still creates dynamic tables when they are first used, even in Verify mode: event-sequence tables for new sequences, unique-constraint tables and read-model tables, including those created when a projection is rebuilt. So the runtime identity still needs rights to create those tables, and you cannot run the kernel with only `db_datareader` and `db_datawriter` on its own tables.

Each new event store and each new namespace needs a database or schema provisioned ahead of its first use. Without it, the first access fails, and that access can happen after the rollout has finished.

## What Chronicle does not provide

Orleans reactivates the grains of a stopped node on remaining nodes, but Chronicle does not guarantee how long that takes or what clients see meanwhile. Test the rollout against a copy of your data before relying on its availability. Readiness checks startup completion and configured storage checks, not every event store and namespace database, so continue monitoring processing after each node joins.

Mixed versions need compatible grain calls. Follow the [source-release requirements](#supported-source-releases) and check for release-specific exceptions; a new major requires every node stopped first. Do not rely on moving a grain back to an old node that lacks what the new version added. To roll back, stop every node before starting the previous version, and restore the database backup if its schema is no longer compatible.

A separate [`--migrate-only` step](configuration/sql-schema-operations.md) applies versioned migrations, but nodes still create dynamic tables on first use, including in Verify mode. Chronicle neither limits migrations to the newest node nor enforces an expand-then-contract sequence, where additive changes precede removal of old structures. Removing or renaming a column can break nodes still running the old version. Keep overlapping releases additive, or stop the cluster for the change. Chronicle does not downgrade schemas; recovery requires a backup.

## See also

- [Run Chronicle on Azure with Microsoft Entra ID](azure.md)
- [Clustering](configuration/clustering.md)
- [Health endpoint](configuration/health-endpoint.md)
- [Storage connection resolution](configuration/storage-connection-resolution.md)
- [Production deployment](production.md)
