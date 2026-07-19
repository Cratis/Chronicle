# Clustering

Clustering configuration controls how multiple Chronicle Server nodes form one Orleans cluster and how grain workloads are distributed across them. This enables horizontal scale out and assigning specific roles to different nodes—for example, dedicating some nodes to event sequences and others to observers (reactors, reducers, projections).

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

When a grain attempts to activate on a node where its role is disabled, the placement director throws an `InvalidOperationException`. Orleans will then attempt placement on other compatible nodes in the cluster.

If no compatible nodes are available (e.g., all nodes have `eventSequences: false`), the grain activation will fail.

## Architecture

Chronicle uses custom Orleans placement strategies to enforce role-based placement:

- **EventSequencePlacementStrategy**: Applied to `EventSequence` grains
- **ObserverPlacementStrategy**: Applied to `Observer` grains (base class for reactors, reducers, and projections)

These strategies consult the `Clustering.Roles` configuration on each silo and select a compatible node using random placement.
