---
description: Configure how multiple Chronicle Server nodes form one Orleans cluster and distribute workloads by role for horizontal scale out.
---
# Clustering

Clustering configuration controls how multiple Chronicle Server nodes form one Orleans cluster and how grain workloads are distributed across them. This enables horizontal scale out and assigning specific roles to different nodes—for example, dedicating some nodes to event sequences and others to observers (reactors, reducers, projections).

> [!WARNING]
> The clustering type defaults to `Localhost`, which is single-node membership, regardless of storage
> type. A deployment with multiple nodes must explicitly select `MongoDB` with MongoDB storage, or `Sql`
> with SQL Server or PostgreSQL storage, on every node. Nodes left on `Localhost` do not join one cluster,
> even when they share storage. The localhost-clustering warning is only logged for non-local MongoDB
> storage; it is not a safeguard for SQL deployments.
>
> All nodes must share the same cluster database, `clusterId` and `serviceId`. Configure the type through
> `clustering.type` in `chronicle.json` or `Cratis__Chronicle__Clustering__Type`. Unsupported combinations
> fail at startup: `Sql` requires `MsSql` or `PostgreSql` storage, and `MongoDB` requires MongoDB storage.
> SQLite and in-memory storage do not support multi-node clustering.

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

### SQL clustering

For SQL Server or PostgreSQL storage, explicitly set `clustering.type` to `Sql`:

```json
{
  "storage": {
    "type": "PostgreSql",
    "connectionDetails": "Host=database;Database=chronicle;Username=chronicle;Password=secret"
  },
  "clustering": {
    "type": "Sql",
    "clusterId": "chronicle",
    "serviceId": "chronicle",
    "defunctSiloCleanupPeriod": "01:00:00",
    "defunctSiloExpiration": "03:00:00"
  }
}
```

For SQL Server, use `storage.type: MsSql` and its connection details instead. Every node must connect to
one shared cluster database (including the same configured SQL schema), not a per-node database.
Membership uses Chronicle's SQL connection resolution and schema provisioning. Silo and gateway ports
must be reachable between nodes; use `advertisedIP` when the default address is not reachable.

The defunct-silo cleanup settings apply to both `Sql` and `MongoDB`: the sweep runs hourly by default
and removes defunct entries older than three hours. Set the cleanup period to `00:00:00` to disable it.
The development kernel state reset is refused under `Sql` clustering because it would erase live
membership. Use an isolated `Localhost` cluster for development resets. To replace nodes of a SQL cluster, see the [rolling-deploy runbook](../rolling-deploy.md).

After a node is lost, observers can lag behind the event log until the observer watchdog catches them up. This applies to every clustering type and is tracked in [Chronicle#5246](https://github.com/Cratis/Chronicle/issues/5246).

## Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| type | string | Localhost | Cluster membership. `Localhost` is single node development clustering. `MongoDB` keeps membership in MongoDB storage. `Sql` keeps membership in the SQL Server or PostgreSQL cluster database. Multi-node clustering is explicit opt-in; all nodes must share the cluster database and cluster identity. |
| siloPort | number | 11111 | Port for silo to silo communication. Must differ per node when multiple nodes run on one machine. |
| gatewayPort | number | 30000 | Port for the silo's client gateway. Must differ per node when multiple nodes run on one machine. |
| clusterId | string | chronicle | The cluster id - all nodes that should form one cluster must share it. |
| serviceId | string | chronicle | The service id - all nodes that should form one cluster must share it. |
| advertisedIP | string | | The IP address the silo advertises to other cluster members. Resolved from the machine's host name when not set; set it explicitly (e.g. `127.0.0.1`) when running multiple nodes on one machine. |
| defunctSiloCleanupPeriod | timespan | 01:00:00 | How often defunct silo entries are swept out of the membership table when using `MongoDB` or `Sql` clustering. Without the sweep, dead entries from restarts and failed rollouts accumulate and slow down or block new nodes joining. Set to `00:00:00` to disable the sweep. |
| defunctSiloExpiration | timespan | 03:00:00 | The age at which a defunct membership entry is removed by the sweep. A node never reuses a silo identity, so dead entries only have diagnostic value. |
| probeTimeout | timespan | Orleans default | Interval between liveness probes and the timeout for each probe. Applies to `MongoDB` and `Sql` clustering. |
| numMissedProbesLimit | number | Orleans default | Missed probes before a silo is suspected to be down. Applies to `MongoDB` and `Sql` clustering. |
| numVotesForDeathDeclaration | number | Orleans default | Non-expired votes needed to declare a silo down. Must not exceed `numProbedSilos`. Applies to `MongoDB` and `Sql` clustering. |
| tableRefreshTimeout | timespan | Orleans default | Interval between fetching membership table updates. Applies to `MongoDB` and `Sql` clustering. |
| iAmAliveTablePublishTimeout | timespan | Orleans default | Interval between publishing this silo's heartbeat to the membership table. Applies to `MongoDB` and `Sql` clustering. |
| numProbedSilos | number | Orleans default | Number of other silos each silo probes for liveness. Applies to `MongoDB` and `Sql` clustering. |
| roles.eventSequences | boolean | true | When `true`, event sequence grains can be activated on this node. When `false`, event sequence grains will not be placed on this node. |
| roles.observers | boolean | true | When `true`, observer grains (reactors, reducers, projections) can be activated on this node. When `false`, observer grains will not be placed on this node. |

The optional membership settings above keep the Orleans defaults when omitted. Time spans use
`hh:mm:ss` format. Configure the same values on every node. Shorter timeouts detect failures sooner,
but can also mark healthy nodes as failed during network delays or heavy load. Orleans validates the
membership options at startup; these settings do not change localhost clustering.

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

With localhost clustering, including SQL deployments that have not opted into `Sql` clustering, the deployment must run one instance at a time. Stop the old instance before starting the new one. In Azure Container Apps, use single revision mode with no overlap between revisions. In Kubernetes, use the `Recreate` deployment strategy, or a rolling update with `maxSurge: 0`.

## Architecture

Chronicle uses custom Orleans placement strategies for role-based placement:

- **EventSequencePlacementStrategy**: applied to `EventSequence` grains and resolved by `EventSequencePlacementDirector`
- **ObserverPlacementStrategy**: applied to `Observer` grains (the base class for reactors, reducers, and projections) and resolved by `ObserverPlacementDirector`

Each director consults the `Clustering.Roles` configuration of the silo it runs on and selects a compatible node using random placement, excluding the local silo when its own role for that grain type is disabled.
