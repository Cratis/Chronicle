---
description: Configure how multiple Chronicle Server nodes form one Orleans cluster and distribute workloads by role for horizontal scale out.
---
# Clustering

Clustering configuration controls how multiple Chronicle Server nodes form one Orleans cluster and how grain workloads are distributed across them. This enables horizontal scale out and assigning specific roles to different nodes—for example, dedicating some nodes to event sequences and others to observers (reactors, reducers, projections).

> [!WARNING]
> `Localhost` means **one active node per kernel storage**, not one independent node per container.
> A single-writer lease now guards this rule by default on MongoDB, PostgreSQL, SQL Server and SQLite.
> A second node waits without activating grains or processing events, then fails startup if its
> acquisition timeout expires. Changing `clusterId` or `serviceId` does not bypass this guard.
>
> For multiple active nodes, use MongoDB storage and set `Cratis__Chronicle__Clustering__Type=MongoDB`
> on every node, with identical `clusterId` and `serviceId`. That mode uses cluster membership rather
> than the single-writer lease. `InMemory` is process-local and never takes a lease.

## Use cases

- **Horizontal scaling**: Run multiple nodes as one cluster; separate event ingestion (event sequences) from event processing (observers) to scale independently based on load
- **Resource isolation**: Run observers on nodes with different resource profiles (e.g., more memory for complex projections)
- **Testing**: Validate multi-node behavior in integration tests by enforcing deterministic grain placement

## Example configuration

```json
{
  "clustering": {
    "type": "MongoDB",
    "siloPort": 11111,
    "gatewayPort": 30000,
    "clusterId": "chronicle",
    "serviceId": "chronicle",
    "roles": {
      "eventSequences": true,
      "observers": true
    }
  }
}
```

## Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| type | string | Localhost | Cluster membership. `Localhost` is single node development clustering. `MongoDB` keeps membership in the configured MongoDB storage - all nodes sharing the same storage and cluster id form one cluster. |
| siloPort | number | 11111 | Port for silo to silo communication. Must differ per node when multiple nodes run on one machine. |
| gatewayPort | number | 30000 | Port for the silo's client gateway. Must differ per node when multiple nodes run on one machine. |
| clusterId | string | chronicle | The cluster id - all nodes that should form one cluster must share it. |
| serviceId | string | chronicle | The service id - all nodes that should form one cluster must share it. |
| advertisedIP | string | | The IP address the silo advertises to other cluster members. Resolved from the machine's host name when not set; set it explicitly (e.g. `127.0.0.1`) when running multiple nodes on one machine. |
| defunctSiloCleanupPeriod | timespan | 01:00:00 | How often defunct silo entries are swept out of the membership table when using `MongoDB` clustering. Without the sweep, dead entries from restarts and failed rollouts accumulate and slow down or block new nodes joining. Set to `00:00:00` to disable the sweep. |
| defunctSiloExpiration | timespan | 03:00:00 | The age at which a defunct membership entry is removed by the sweep. A node never reuses a silo identity, so dead entries only have diagnostic value. |
| singleWriterLease.enabled | boolean or null | null (automatic) | Enables the lease for `Localhost` on persistent storage. Set `false` only when another mechanism guarantees one writer. Does not enable a lease for `MongoDB` clustering or `InMemory` storage. |
| singleWriterLease.ttl | timespan | 00:00:30 | Database lease lifetime; minimum five seconds. |
| singleWriterLease.acquireTimeout | timespan | 00:02:00 | Maximum startup wait. Zero makes one acquisition attempt; negative values are invalid. |
| roles.eventSequences | boolean | true | When `true`, event sequence grains can be activated on this node. When `false`, event sequence grains will not be placed on this node. |
| roles.observers | boolean | true | When `true`, observer grains (reactors, reducers, projections) can be activated on this node. When `false`, observer grains will not be placed on this node. |

## Single-writer lease

The node acquires the fixed `kernel` lease in the system/cluster database **before** Orleans runtime
initialization. No grains, observer rehydration or processing start until acquisition succeeds. Waiting
nodes are not ready. Acquisition waits at most `acquireTimeout`; the timeout error identifies the last
known holder, host and expiry. Temporary storage failures are retried within that budget.

The holder renews every TTL/3. If it cannot confirm a renewal for TTL/2 (15 seconds by default), it marks
readiness unhealthy, logs at Critical level and requests shutdown with a nonzero process exit code. A
watchdog forces process termination before the original TTL expires if graceful shutdown gets stuck.
A renewal rejected because ownership was lost fences the process immediately. Set the TTL so that half
of it comfortably exceeds the host's shutdown timeout. Your restart policy must restart failed processes.

During normal shutdown, renewal continues until the silo has stopped its grains and runtime services;
then the lease is released. After SIGKILL, a crash or a lost release, a successor waits at most the
remaining TTL rather than requiring manual cleanup. The lease row/document remains for diagnostics;
expiry is not driven by a MongoDB TTL index.

PostgreSQL, SQL Server and MongoDB use **database server time** for acquisition, renewal and expiry, not
the application's wall clock. MongoDB requires version 5.0 or later for server-time `$dateAdd` update pipelines.
SQLite uses the host clock: sharing a SQLite file across hosts with unsynchronized clocks is unsupported,
as are network shares without reliable file locking.

Disabling `singleWriterLease.enabled` preserves unguarded single-node behavior. If storage is remote,
the server warns about the risk, including for PostgreSQL and SQL Server. This is an escape hatch, not a
way to scale out. See [Production scaling](../production.md#scaling) for safe replacement deployments.

## Configuration examples

### Default (all roles enabled)

By default, all roles are enabled on every node. This is the standard single-node or homogeneous multi-node configuration:

```json
{
  "clustering": {
    "roles": {
      "eventSequences": true,
      "observers": true
    }
  }
}
```

### Dedicated event sequence node

A node that only handles event sequences (event ingestion and appending):

```json
{
  "clustering": {
    "roles": {
      "eventSequences": true,
      "observers": false
    }
  }
}
```

### Dedicated observer node

A node that only processes observers (reactors, reducers, projections):

```json
{
  "clustering": {
    "roles": {
      "eventSequences": false,
      "observers": true
    }
  }
}
```

## Behavior

Role-based placement is applied by custom Orleans placement directors - one for event sequence grains and one for observer grains. When a grain needs to be activated, the director running on the silo that makes the placement decision:

1. Starts from the silos Orleans reports as compatible with the grain type.
2. Removes **this** silo from the candidates when this silo has the grain's role disabled.
3. Selects one of the remaining candidates at random.
4. Throws an `InvalidOperationException` when no candidate remains - for example, when every silo has that role disabled.

> [!NOTE]
> A placement director only sees the role configuration of the silo it runs on. It can keep a grain off the local silo when the local role is disabled, but it does not currently guarantee that a grain is never placed on a *remote* silo whose role is disabled. Role-based isolation is therefore best-effort today - treat the role settings as a scheduling hint rather than a hard cluster-wide guarantee, and run homogeneous nodes (all roles enabled, the default) when every node must accept every grain.

## Deploying a single node

With localhost clustering, which is the only option with SQL storage today, the deployment must run one instance at a time. Stop the old instance before starting the new one. In Azure Container Apps, use single revision mode with no overlap between revisions. In Kubernetes, use the `Recreate` deployment strategy, or a rolling update with `maxSurge: 0`.

## Architecture

Chronicle uses custom Orleans placement strategies for role-based placement:

- **EventSequencePlacementStrategy**: applied to `EventSequence` grains and resolved by `EventSequencePlacementDirector`
- **ObserverPlacementStrategy**: applied to `Observer` grains (the base class for reactors, reducers, and projections) and resolved by `ObserverPlacementDirector`

Each director consults the `Clustering.Roles` configuration of the silo it runs on and selects a compatible node using random placement, excluding the local silo when its own role for that grain type is disabled.
