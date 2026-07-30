# Clustering

Clustering configuration controls how multiple Chronicle Server nodes form one Orleans cluster and how grain workloads are distributed across them. This enables horizontal scale out and assigning specific roles to different nodes—for example, dedicating some nodes to event sequences and others to observers (reactors, reducers, projections).

> [!WARNING]
> The clustering type defaults to `Localhost`, which is single-node membership. Any production deployment
> that runs more than one node **must** set the type to `MongoDB` on every node. Two nodes left on the
> default that share the same MongoDB do **not** join as one cluster - each forms its own isolated
> single-node cluster over the same data, a split-brain topology reported by no error at startup. Set it
> with the `Cratis__Chronicle__Clustering__Type=MongoDB` environment variable (or `clustering.type` in
> `chronicle.json`), and give every node the same `clusterId` and `serviceId`. The server logs a warning
> when it detects localhost clustering against non-local storage, but it does not refuse to start.

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
| roles.eventSequences | boolean | true | When `true`, event sequence grains can be activated on this node. When `false`, event sequence grains will not be placed on this node. |
| roles.observers | boolean | true | When `true`, observer grains (reactors, reducers, projections) can be activated on this node. When `false`, observer grains will not be placed on this node. |

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

## Architecture

Chronicle uses custom Orleans placement strategies for role-based placement:

- **EventSequencePlacementStrategy**: applied to `EventSequence` grains and resolved by `EventSequencePlacementDirector`
- **ObserverPlacementStrategy**: applied to `Observer` grains (the base class for reactors, reducers, and projections) and resolved by `ObserverPlacementDirector`

Each director consults the `Clustering.Roles` configuration of the silo it runs on and selects a compatible node using random placement, excluding the local silo when its own role for that grain type is disabled.
