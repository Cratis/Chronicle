# Clustering

Clustering configuration controls how Chronicle Server distributes grain workloads across multiple nodes in an Orleans cluster. This enables you to assign specific roles to different nodes—for example, dedicating some nodes to event sequences and others to observers (reactors, reducers, projections).

## Use cases

- **Horizontal scaling**: Separate event ingestion (event sequences) from event processing (observers) to scale independently based on load
- **Resource isolation**: Run observers on nodes with different resource profiles (e.g., more memory for complex projections)
- **Testing**: Validate multi-node behavior in integration tests by enforcing deterministic grain placement

## Example configuration

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

## Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
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
