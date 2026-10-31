---
title: Roll out a new version to a multi-node Chronicle cluster
description: Check whether two Chronicle versions can share a cluster, replace the nodes one at a time, know what each starting node does to a SQL database, and see which guarantees Chronicle does not give you.
---

This guide shows an operator how to replace the nodes of a Chronicle kernel cluster one node at a time. You finish knowing whether the two versions can run side by side at all, with a rollout order, probe settings and a list of things to watch while it runs.

[Check that the two versions can share a cluster](#check-that-the-two-versions-can-share-a-cluster) applies to every multi-node cluster, whether it uses `MongoDB` or `Sql` clustering. The rest of the guide covers Azure SQL (`MsSql`) and Azure Database for PostgreSQL (`PostgreSql`) with `Cratis__Chronicle__Clustering__Type=Sql`, where a starting node can also change the database schema. Chronicle makes no zero-downtime promise for a rollout. Read [What Chronicle does not provide](#what-chronicle-does-not-provide) before you rely on one.

## Prerequisites

- A running multi-node cluster, such as one set up as described in [Plan for availability](azure.md#plan-for-availability): the same clustering type on every node, one shared cluster database, and the same `clusterId` and `serviceId`.
- A way to replace nodes one at a time, such as a Kubernetes `Deployment` or an Azure Container Apps revision with controlled overlap.
- A backup of the database taken before the rollout. Chronicle can apply schema migrations when a new node starts, and it has no command to roll them back.

## Supported source releases

When upgrading from 19.114.1 or earlier, stop all nodes, or keep public client traffic off the cluster until every node runs 19.115.0.

Rolling upgrades are supported from 19.111 onward. Upgrading from an older release needs a stop-all upgrade: stop every silo, then start the new release. Older releases lack the patch-manager handover or have changed grain interfaces that prevent the old and new silos from exchanging calls.

## Check that the two versions can share a cluster

While nodes are being replaced, nodes of the old version and of the new one form one Orleans cluster and call each other's grains. That only works when the new version still serves every grain call the old version makes. When it does not, the first new node stops during startup, and the rollout stalls with the old nodes still running.

Use this table to decide how to upgrade:

| Upgrading | Within one major | How to upgrade |
| --- | --- | --- |
| From 19.111 or later | Yes | Replace nodes one at a time. Releases are checked in CI against the release before them. |
| From a 19.x release before 19.111 | Yes | Stop every node, then start the new version. Older releases lack the patch-manager handover or have incompatible grain calls. |
| To a new major | No | Stop every node, then start the new version. A major release may change grain calls. |

To stop every node in Kubernetes, scale the deployment to zero and back, or use the `Recreate` strategy for that one upgrade. In Azure Container Apps, use single revision mode with no overlap. Stopping the old nodes first is safe; it is the overlap that fails.

A new node that meets an incompatible version logs a critical message naming the startup step and saying a full stop is needed, then stops:

```text
Startup step 'DiscoverAndRegisterKernelProjections' failed because a silo in this cluster most likely runs a Chronicle release this one cannot exchange grain calls with.
```

The exception beneath it is a `TypeLoadException` such as `Unable to resolve type alias "("inv",[Orleans.Runtime.GrainReference],[Cratis.Chronicle.Projections.IProjectionsManager,Cratis.Chronicle.Core],"F436C56F")"`. Versions before 19.107.0 show only the exception. Either way, roll back the new node, then stop every node and start the new version.

## Roll out administrator authorization

Keep `Authentication.EnforceAdministratorPolicyForOperations=false` on every node while replacing nodes
with the administrator-policy release. Existing operational calls then remain in audit mode. The new
administrator-management service is always enforced on upgraded nodes when authentication is enabled;
old nodes do not expose that service.

After every node has been upgraded, review the would-deny warnings for user/application management,
System job recovery and System observer recovery. Confirm that the operators who need those operations
are active administrators, and obtain fresh user sessions carrying `chronicle_principal=user`. Refresh
OpenIddict user tokens or sign in again for existing cookie and Identity bearer sessions. Applications
cannot acquire administrator privilege, even if their token subject matches an administrator's identifier.

Only then set `Authentication.EnforceAdministratorPolicyForOperations=true` consistently across the
cluster. During a staggered configuration change, a call can still be audited rather than denied on a
node that retains audit mode; enforcement is not cluster-wide until every node has the setting. Application-store
operations are unaffected. See [Administrator authorization](configuration/authentication.md#administrator-authorization)
for the configuration and external-authority limitations.

Every release that includes the administrator roster must recognize `GrantReason.AdministratorGrant`
(value `3`) before administrator-management grants are written. A node whose enum converter does not
recognize a persisted reason cannot safely replay that history. Do not roll back to such a version after
writing those grants. A roster without active administrators must be recovered through the server-local
recovery configuration, not by disabling policy enforcement on a reachable node.

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

During the rollout, nodes on the old version and on the new version run in the same Orleans cluster and call each other's grains. When the [two versions can share a cluster](#check-that-the-two-versions-can-share-a-cluster), Orleans routes each call to a node that understands it, and a grain may move from an old node to a new one partway through the rollout. Keep the window short anyway, and do not leave a cluster in a mixed state overnight: a grain that moves is deactivated and reactivated, and every move costs time.

### 4. Give nodes time to start and stop

A node binds its listeners before starting its silo, so a schema lock wait or startup retries do not prevent startup and liveness probes from succeeding. Keep the node out of traffic until readiness succeeds.

- **Startup probe.** Use `tcpSocket` on port 35000 or an HTTP probe on `/health/live`. Raising `failureThreshold` is not the fix for backlog-dependent startup restarts; do not use readiness as the startup probe.
- **Liveness probe.** Use `/health/live`, which remains successful while the silo is starting, including while waiting on a schema lock.
- **Shutdown.** The host allows 10 seconds for a graceful stop (`HostOptions.ShutdownTimeout`). Set the termination grace period at or above that and let the orchestrator send `SIGTERM`. The server treats a signal during startup as an orderly stop.
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

This option stops Chronicle creating databases and schemas only. **Migrations still run in the existing databases**, so the identity Chronicle migrates as still needs rights to create and alter tables in its schema. The `Schema` and `Runtime` identities from [connection resolution](configuration/storage-connection-resolution.md#what-chronicle-asks) can be different logins, but both are used by the kernel process. Chronicle has no separate job that migrates the schema ahead of time, so you cannot run the kernel with only `db_datareader` and `db_datawriter` on its own tables.

Each new event store and each new namespace needs a database or schema provisioned ahead of its first use. Without it, the first access fails, and that access can happen after the rollout has finished.

## What Chronicle does not provide

- **No zero-downtime guarantee.** Orleans reactivates the grains of a stopped node on the remaining nodes, but Chronicle does not state how long that takes or what clients see meanwhile. Test a rollout against a copy of your data before relying on it.
- **No mixed versions across a major, or from before 19.106.** Grain calls are kept compatible within a major from 19.106 onwards. Other upgrades need every node stopped first.
- **No rollback while mixed.** A grain that moved to a new node cannot move back to an old node that lacks what the new version added. Roll back by stopping every node and starting the previous version.
- **No migration job and no migrate-only mode.** The nodes migrate.
- **No schema version gate.** Chronicle does not enforce expand-then-contract database migrations, or limit migrations to the newest node. A change that removes or renames a column breaks nodes still running the old version. Plan those releases as a cluster stop and start, or keep each release additive.
- **No readiness check for membership or startup.** See step 5.
- **No schema downgrade.** Restore from backup.

## See also

- [Run Chronicle on Azure with Microsoft Entra ID](azure.md)
- [Clustering](configuration/clustering.md)
- [Health endpoint](configuration/health-endpoint.md)
- [Storage connection resolution](configuration/storage-connection-resolution.md)
- [Production deployment](production.md)
